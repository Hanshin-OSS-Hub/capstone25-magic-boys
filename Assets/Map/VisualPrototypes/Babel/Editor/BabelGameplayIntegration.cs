#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// 생성 규칙과 물리 메시를 보존한 채, 승인된 시안을 실제 던전에 연결한다.
public static class BabelGameplayIntegration
{
    public const string ScenePath = "Assets/Map/Scenes/0326_MapMerge.unity";
    public const string Output = "Assets/Map/BabelGameplay";
    const string Art = "Assets/Map/VisualPrototypes/Babel/AssetDraft";
    public const string DwarfRoot = Art + "/Dwarf";
    static readonly List<string> report = new List<string>();

    public static void Build()
    {
        Directory.CreateDirectory(Output + "/Rooms");
        Directory.CreateDirectory(Output + "/Meshes");
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var generator = Object.FindFirstObjectByType<DungeonGenerator>();
        var serialized = new SerializedObject(generator);
        var cache = new Dictionary<string, GameObject>();
        foreach (string field in new[] { "tilePrefabs", "startPrefabs", "exitPrefabs", "blockedPrefabs", "doorPrefabs", "puzzlePrefabs" })
        {
            var array = serialized.FindProperty(field);
            for (int i = 0; i < array.arraySize; i++)
            {
                var entry = array.GetArrayElementAtIndex(i);
                string path = AssetDatabase.GetAssetPath(entry.objectReferenceValue);
                if (path.StartsWith(Output)) continue;
                if (!cache.TryGetValue(path, out var variant))
                {
                    variant = MakeRoom(path);
                    cache.Add(path, variant);
                }
                entry.objectReferenceValue = variant;
            }
        }
        serialized.FindProperty("useLightsForDebugging").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var player = GameObject.FindGameObjectWithTag("Player");
        ApplyDwarf(player);
        // 시작 방의 기존 보상 큐브: 기능/판정은 그대로 두고 회색 기본 재질만 교체.
        foreach(var chest in Object.FindObjectsByType<BreakableChest>(FindObjectsSortMode.None))
            if(chest.name=="Cube"&&chest.TryGetComponent<MeshRenderer>(out var chestRenderer))
                chestRenderer.sharedMaterial=Material("M_Dark_Wood");
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.17f,.21f,.27f);
        RenderSettings.ambientEquatorColor = new Color(.105f,.135f,.18f);
        RenderSettings.ambientGroundColor = new Color(.065f,.08f,.105f);
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional) { light.intensity = .22f; light.color = Color.white; }
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllLines("WhisperServer/.runtime/babel-gameplay-integration.txt", report);
        Debug.Log("[Babel Gameplay] " + string.Join("\n", report));
    }

    static GameObject MakeRoom(string path)
    {
        var original = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var room = (GameObject)PrefabUtility.InstantiatePrefab(original);
        PrefabUtility.UnpackPrefabInstance(room, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        string id = AssetDatabase.AssetPathToGUID(path).Substring(0,8);
        room.name = original.name + "_Babel";
        int colliderCount = room.GetComponentsInChildren<Collider>(true).Length;
        var physicsMeshes = room.GetComponentsInChildren<MeshCollider>(true).Select(c=>c.sharedMesh).ToArray();
        var connectors = room.GetComponentsInChildren<Connector>(true).Select(c=>c.transform.localPosition).ToArray();
        bool decorated = path == "Assets/Map/Prefabs/Tiles/Room I/Room I.prefab";
        if (decorated)
        {
            var approved = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/RoomI_Babel_Assets.prefab");
            room.GetComponent<MeshRenderer>().enabled = false;
            foreach (Transform child in approved.transform)
                if (child.name.StartsWith("Babel_Surface_") || child.name == "Babel_Asset_Visuals_NoCollision")
                    Object.Instantiate(child.gameObject, room.transform, false).name = child.name;
            foreach (Light light in room.GetComponentsInChildren<Light>(true))
            {
                bool art = light.transform.IsChildOf(room.transform.Find("Babel_Asset_Visuals_NoCollision"));
                light.enabled = art;
                if (art) light.intensity = light.name.Contains("Neutral") ? 8f : 6.9f;
            }
        }
        else
        {
            // 원래 방의 삼각형과 크기는 그대로, 표시용 UV/재질만 분리한다.
            var filter = room.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                var source = filter.sharedMesh;
                var vertices = source.vertices; var normals = source.normals;
                var positions = new List<Vector3>(); var ns = new List<Vector3>(); var uv = new List<Vector2>();
                var groups = new[] { new List<int>(), new List<int>(), new List<int>() };
                var ts = source.triangles;
                for (int t=0;t<ts.Length;t+=3)
                {
                    var normal = Vector3.Cross(vertices[ts[t+1]]-vertices[ts[t]],vertices[ts[t+2]]-vertices[ts[t]]).normalized;
                    int group = normal.y > .6f ? 0 : normal.y < -.6f ? 2 : 1;
                    for(int k=0;k<3;k++)
                    {
                        int index=ts[t+k];var p=vertices[index]; groups[group].Add(positions.Count);
                        positions.Add(p);ns.Add(normals.Length>index?normals[index]:normal);
                        uv.Add(Mathf.Abs(normal.y)>.6f?new Vector2(p.x,p.z)*.35f:Mathf.Abs(normal.x)>Mathf.Abs(normal.z)?new Vector2(p.z,p.y)*.38f:new Vector2(p.x,p.y)*.38f);
                    }
                }
                var mesh = new Mesh {name=room.name+"_Visual", indexFormat=IndexFormat.UInt32};
                mesh.SetVertices(positions);mesh.SetNormals(ns);mesh.SetUVs(0,uv);mesh.subMeshCount=3;
                for(int g=0;g<3;g++)mesh.SetTriangles(groups[g],g);
                mesh.RecalculateBounds();mesh.RecalculateTangents();
                string meshPath=Output+"/Meshes/"+id+".asset";
                var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(old==null)AssetDatabase.CreateAsset(mesh,meshPath);else {EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}
                filter.sharedMesh=mesh;
                room.GetComponent<MeshRenderer>().sharedMaterials=new[]{Material("M_Stone_Slabs"),Material("M_Limestone_Blocks"),Material("M_Carved_Stone")};
            }
            foreach(var light in room.GetComponentsInChildren<Light>(true))
            {
                light.color=new Color(.93f,.97f,1);light.intensity=Mathf.Min(light.intensity,8f);
            }
        }
        if(colliderCount!=room.GetComponentsInChildren<Collider>(true).Length ||
            !physicsMeshes.SequenceEqual(room.GetComponentsInChildren<MeshCollider>(true).Select(c=>c.sharedMesh)) ||
            !connectors.SequenceEqual(room.GetComponentsInChildren<Connector>(true).Select(c=>c.transform.localPosition)))
            throw new Exception("방 물리/연결점 변경 감지: "+path);
        string destination=Output+"/Rooms/"+original.name.Replace("/","_")+"_"+id+".prefab";
        var saved=PrefabUtility.SaveAsPrefabAsset(room,destination);
        report.Add($"{path} → {destination}: collider={colliderCount}, connector={connectors.Length}, 원본 물리 메시 유지");
        Object.DestroyImmediate(room);return saved;
    }

    static Material Material(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/"+name+".mat");

    public static void Capture(Camera camera, string path)
    {
        bool previousAsync = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;
        var target = new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
        target.Create();
        var request = new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination=target };
        RenderPipeline.SubmitRenderRequest(camera,request);RenderPipeline.SubmitRenderRequest(camera,request);
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=previous;Object.DestroyImmediate(image);target.Release();Object.DestroyImmediate(target);
        ShaderUtil.allowAsyncCompilation=previousAsync;
    }

    public static void InspectSurfaces()
    {
        var lines=new List<string>();
        foreach(string path in Directory.GetFiles(Output+"/Rooms","*.prefab"))
        {
            var room=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var renderer in room.GetComponentsInChildren<MeshRenderer>(true))
                lines.Add(path+" | "+AnimationUtility.CalculateTransformPath(renderer.transform,room.transform)+" | "+string.Join(",",renderer.sharedMaterials.Select(m=>m!=null?m.name:"NULL")));
        }
        File.WriteAllLines("WhisperServer/.runtime/babel-surface-inspect.txt",lines);
    }

    public static void CleanRetiredPrototypes()
    {
        const string prototype="Assets/Map/VisualPrototypes/";
        // 다른 씬/프리팹의 역참조까지 보존. 승인된 비교 씬도 계속 사용할 수 있다.
        var roots=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!p.StartsWith(prototype)&&
            (p.EndsWith(".unity")||p.EndsWith(".prefab")||p.EndsWith(".asset")||p.EndsWith(".mat"))).ToList();
        roots.AddRange(new[]{DwarfRoot+"/Babel_Dwarf_Compare.unity",DwarfRoot+"/Player_Babel_Dwarf.prefab",Art+"/RoomI_Babel_Assets.prefab"});
        var keep=new HashSet<string>(AssetDatabase.GetDependencies(roots.ToArray(),true));
        var removed=new List<string>();long bytes=0;
        foreach(string path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith(prototype)&&File.Exists(p)).ToArray())
        {
            if(keep.Contains(path)||path.Contains("/Editor/BabelGameplay")||path.EndsWith("/Editor/BabelDwarfVerification.cs")||path.EndsWith("/Editor/rig_dwarf.py")||
                path.StartsWith(DwarfRoot+"/Source/")||path.StartsWith(DwarfRoot+"/Textures/")||path.EndsWith("rig-report.json")||
                path==DwarfRoot+"/README.md"||path==Art+"/README.md"||path==Art+"/THIRD_PARTY.md"||path.EndsWith("/Textures/SOURCES.json"))continue;
            string full=Path.GetFullPath(path),root=Path.GetFullPath(prototype);
            if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new Exception("정리 경계 오류");
            string backup="WhisperServer/.runtime/retired-visuals/"+path;
            Directory.CreateDirectory(Path.GetDirectoryName(backup));File.Copy(path,backup,true);
            if(File.Exists(path+".meta"))File.Copy(path+".meta",backup+".meta",true);
            bytes+=new FileInfo(path).Length;
            if(!AssetDatabase.DeleteAsset(path))throw new Exception("정리 실패: "+path);
            removed.Add(path);
        }
        File.WriteAllLines("WhisperServer/.runtime/retired-visuals-manifest.txt",removed);
        Debug.Log($"[Babel Cleanup] 미참조 파일 {removed.Count}개, {bytes} bytes를 Git 제외 로컬 보관 후 Assets에서 제거");
    }

    static void ApplyDwarf(GameObject player)
    {
        if(player.GetComponent<BabelDwarfMotion>()!=null){report.Add("드워프 이미 연결됨");return;}
        var prototype=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Dwarf/Player_Babel_Dwarf.prefab").GetComponent<BabelMageAppearance>();
        var rootAnimator=player.GetComponent<Animator>();
        var avatar=rootAnimator.avatar;var controller=rootAnimator.runtimeAnimatorController;
        int colliders=player.GetComponentsInChildren<Collider>(true).Length;
        var clothing=Object.Instantiate(prototype.clothing,player.transform,false);clothing.name=prototype.clothing.name;
        var app=player.AddComponent<BabelMageAppearance>();
        app.clothing=clothing;
        app.originalBody=player.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Armature_Mesh");
        app.dressedBody=clothing.GetComponentsInChildren<Renderer>(true).Single(r=>r.name==prototype.dressedBody.name);
        app.headRenderers=prototype.headRenderers.Select(r=>clothing.transform.Find(AnimationUtility.CalculateTransformPath(r.transform,prototype.clothing.transform)).GetComponent<Renderer>()).ToArray();
        app.hammer=player.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Human_Warhammer");
        app.hammerMeshFilter=app.hammer.GetComponent<MeshFilter>();
        app.originalHammerMesh=app.hammerMeshFilter.sharedMesh;app.originalHammerMaterials=app.hammer.sharedMaterials;
        app.dressedHammerMesh=prototype.dressedHammerMesh;app.mageHammerMaterials=prototype.mageHammerMaterials;
        app.Show(true);
        var sync=player.AddComponent<BabelDwarfMotion>();sync.gameplay=rootAnimator;sync.visual=clothing.GetComponent<Animator>();sync.appearance=app;sync.weapon=app.hammer.transform;
        rootAnimator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        if(rootAnimator.avatar!=avatar||rootAnimator.runtimeAnimatorController!=controller||colliders!=player.GetComponentsInChildren<Collider>(true).Length)
            throw new Exception("플레이어 게임플레이 리그/충돌체 변경");
        report.Add("플레이어: 기존 객체/Animator/Avatar/무기 객체/충돌체 보존, 드워프 시각 리그 연결");
    }
}
#endif
