#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 발표 시연용 개선 작업을 자동으로 적용하는 에디터 도구.
// 메뉴: Tools/Magic Boys/...  (여러 번 실행해도 결과는 같다)
public static class DemoPolishSetup
{
    // ───────────────────────── 3단 콤보 ─────────────────────────
    const string ControllerPath = "Assets/Player/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";
    const string ComboSourceFbx = "Assets/Player/Ch19_nonPBR@Standing Melee Combo Attack Ver. 2 (2).fbx";
    const string ComboClipFolder = "Assets/Player/Animations/Combo";

    // 원본 4.2초 콤보 클립을 손 속도 분석으로 3개의 타격 구간으로 나눔
    static readonly string[] ComboNames = { "Attack1", "Attack2", "Attack3" };
    static readonly float[] ComboStart = { 0.35f, 1.30f, 2.10f };
    static readonly float[] ComboEnd = { 1.30f, 2.10f, 3.90f };
    static readonly float[] ComboSpeed = { 1.2f, 1.2f, 1.1f };

    // ───────────────────────── 던전 크기 ─────────────────────────
    const string RoomFolder = "Assets/Map/BabelGameplay/Rooms";
    const string NavMeshFolder = "Assets/Map/BabelGameplay/NavMesh";
    public static float DungeonScale = 1.4f;

    [MenuItem("Tools/Magic Boys/Apply All Demo Polish")]
    public static void ApplyAll()
    {
        SetupAttackCombo();
        ScaleDungeon();
    }

    [MenuItem("Tools/Magic Boys/Setup Attack Combo")]
    public static void SetupAttackCombo()
    {
        var src = AssetDatabase.LoadAllAssetsAtPath(ComboSourceFbx)
            .OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
        if (src == null)
        {
            Debug.LogError("[DemoPolish] 콤보 원본 클립을 찾지 못했습니다: " + ComboSourceFbx);
            return;
        }

        EnsureFolder(ComboClipFolder);

        var clips = new AnimationClip[ComboNames.Length];
        for (int i = 0; i < ComboNames.Length; i++)
            clips[i] = CreateTrimmedClip(src, ComboStart[i], ComboEnd[i], $"{ComboClipFolder}/Player_{ComboNames[i]}.anim");

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var sm = ctrl.layers[0].stateMachine;

        AnimatorState Find(string n) => sm.states.Select(s => s.state).FirstOrDefault(s => s.name == n);
        var blend = Find("Idle Walk Run Blend");
        var roll = Find("Roll");
        var oldAttack = Find("Attack");
        Vector3 basePos = sm.states.FirstOrDefault(s => s.state == oldAttack).position;

        for (int i = 0; i < ComboNames.Length; i++)
        {
            var st = Find(ComboNames[i]) ?? sm.AddState(ComboNames[i], basePos + new Vector3(250f, 60f * (i - 1), 0f));
            st.motion = clips[i];
            st.speed = ComboSpeed[i];
            st.tag = "Attack";
            st.iKOnFeet = false;
            st.writeDefaultValues = oldAttack != null ? oldAttack.writeDefaultValues : true;

            foreach (var t in st.transitions.ToArray()) st.RemoveTransition(t);

            // 다음 타 입력이 없으면 자연스럽게 기본 이동 상태로 복귀
            var toBlend = st.AddTransition(blend);
            toBlend.hasExitTime = true;
            toBlend.exitTime = 0.95f;
            toBlend.hasFixedDuration = true;
            toBlend.duration = 0.2f;

            // 공격 중 구르기 캔슬
            var toRoll = st.AddTransition(roll);
            toRoll.hasExitTime = false;
            toRoll.hasFixedDuration = true;
            toRoll.duration = 0.05f;
            toRoll.AddCondition(AnimatorConditionMode.If, 0, "Roll");
        }

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log("[DemoPolish] 3단 콤보 상태(Attack1~3) 설정 완료");
    }

    static AnimationClip CreateTrimmedClip(AnimationClip src, float t0, float t1, string path)
    {
        var dst = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool isNew = dst == null;
        if (isNew) dst = new AnimationClip();
        else dst.ClearCurves();

        dst.frameRate = src.frameRate;
        float step = 1f / src.frameRate;
        float len = t1 - t0;

        foreach (var b in AnimationUtility.GetCurveBindings(src))
        {
            var c = AnimationUtility.GetEditorCurve(src, b);
            if (c == null) continue;

            var keys = new List<Keyframe>();
            for (float t = 0f; t < len - step * 0.5f; t += step)
                keys.Add(new Keyframe(t, c.Evaluate(t0 + t)));
            keys.Add(new Keyframe(len, c.Evaluate(t1)));

            var nc = new AnimationCurve(keys.ToArray());
            for (int k = 0; k < nc.length; k++)
            {
                AnimationUtility.SetKeyLeftTangentMode(nc, k, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(nc, k, AnimationUtility.TangentMode.ClampedAuto);
            }
            AnimationUtility.SetEditorCurve(dst, b, nc);
        }

        var s = AnimationUtility.GetAnimationClipSettings(src);
        s.loopTime = false;
        s.startTime = 0f;
        s.stopTime = len;
        AnimationUtility.SetAnimationClipSettings(dst, s);

        if (isNew) AssetDatabase.CreateAsset(dst, path);
        else EditorUtility.SetDirty(dst);
        return dst;
    }

    // ───────────────────────── 던전 전체 크기 ─────────────────────────
    // 천장만 올리면 비율이 어색하므로, 방/복도 구조(벽·바닥·천장·문)를 같은 비율로 키우고
    // 소품(통, 탁자, 횃불 등)은 원래 크기를 유지해서 캐릭터 대비 공간만 넓어 보이게 한다.
    // 문/막힌 벽은 생성기가 연결점(Connector)의 자식으로 붙이므로 자동으로 같이 커진다.
    [MenuItem("Tools/Magic Boys/Scale Dungeon (1.4x)")]
    public static void ScaleDungeon()
    {
        ScaleDungeon(DungeonScale);
    }

    public static void ScaleDungeon(float scale)
    {
        EnsureFolder(NavMeshFolder);
        var log = new StringBuilder();
        string activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;

        foreach (var path in Directory.GetFiles(RoomFolder, "*.prefab").Select(p => p.Replace('\\', '/')).OrderBy(p => p))
        {
            string file = Path.GetFileNameWithoutExtension(path);
            bool isTile = file.StartsWith("Hall") || file.StartsWith("Room") || file.StartsWith("Odd Room");
            if (!isTile) continue; // 문/막힌 벽(연결점 따라 자동 확대), 보스맵(별도 아레나) 제외

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (Mathf.Abs(prefab.transform.localScale.x - 1f) > 0.001f)
            {
                log.AppendLine($"{file}: 이미 {prefab.transform.localScale.x:0.##}배 (건너뜀)");
                continue;
            }

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var root = inst.transform;
            root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // 확대 전 소품 크기 측정
            var shellNames = new[] { "Babel_Surface" };
            var candidates = new List<(Transform t, Bounds b)>();
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var t = r.transform;
                if (t == root || shellNames.Any(n => t.name.StartsWith(n))) continue;
                candidates.Add((t, r.bounds));
            }

            root.localScale = Vector3.one * scale;

            int kept = 0, pillars = 0;
            var done = new HashSet<Transform>();
            foreach (var (t, b) in candidates.OrderBy(c => Depth(c.t)))
            {
                if (done.Contains(t) || HasAncestorIn(t, done)) continue;
                // 구조물(바닥/벽/기둥 조각, 큰 오브젝트, 바닥~천장 기둥)은 방과 같이 키움
                if (IsStructural(t, b)) { done.Add(t); pillars++; continue; }
                t.localScale /= scale;
                done.Add(t);
                kept++;
            }

            foreach (var l in inst.GetComponentsInChildren<Light>(true))
                l.range *= scale;

            string navInfo = "";
            var surface = inst.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            if (surface != null)
            {
                surface.BuildNavMesh();
                string navPath = $"{NavMeshFolder}/NavMesh-{Sanitize(file)}.asset";
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(navPath) != null)
                    AssetDatabase.DeleteAsset(navPath);
                AssetDatabase.CreateAsset(surface.navMeshData, navPath);
                surface.navMeshData = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(navPath);
                navInfo = ", NavMesh 재생성";
            }

            PrefabUtility.ApplyPrefabInstance(inst, InteractionMode.AutomatedAction);
            log.AppendLine($"{file}: {scale:0.##}배 (소품 원래 크기 유지 {kept}, 구조물 확대 {pillars}{navInfo})");
        }

        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(activeScene))
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(activeScene);
        Debug.Log("[DemoPolish] 던전 크기 조정 완료\n" + log);
    }

    static readonly string[] StructuralPrefixes = { "Floor", "Wall", "Pillar", "Column", "Ceiling", "Beam", "Arch", "Stair", "Step", "Babel_Surface" };

    static bool IsStructural(Transform t, Bounds b)
    {
        for (var p = t; p != null && p.parent != null; p = p.parent)
            if (StructuralPrefixes.Any(n => p.name.StartsWith(n))) return true;
        if (b.min.y < 0.6f && b.max.y >= 4.4f) return true;            // 바닥~천장
        if (Mathf.Max(b.size.x, b.size.z) >= 2.5f) return true;        // 큰 바닥 장식/구조물
        if (b.size.y >= 3f) return true;
        return false;
    }

    // ───────────────────────── 유틸 ─────────────────────────
    static int Depth(Transform t) { int d = 0; while (t.parent != null) { d++; t = t.parent; } return d; }

    static bool HasAncestorIn(Transform t, HashSet<Transform> set)
    {
        for (var p = t.parent; p != null; p = p.parent) if (set.Contains(p)) return true;
        return false;
    }

    static string Sanitize(string n) => string.Concat(n.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) || ch == ' ' ? '_' : ch));

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif
