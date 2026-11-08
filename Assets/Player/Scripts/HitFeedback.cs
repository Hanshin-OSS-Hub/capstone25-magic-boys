using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public enum HitElement { Physical, Fire, Water, Earth, Thunder }

// 적이 공격/스킬에 맞았을 때의 타격 연출.
// 데미지 숫자 + 속성별 파편 + 밀려남 + 몸이 움찔하는 스쿼시.
// 사용: HitFeedback.Play(맞은 콜라이더, 맞은 위치, 데미지, 속성, 밀어낼 방향, 밀어낼 힘);
public static class HitFeedback
{
    public static bool Enabled = true;

    private static HitFeedbackRunner runner;
    private static readonly System.Collections.Generic.Dictionary<Transform, (Vector3 baseScale, int token)> squashState = new();
    private static Material particleMaterial;

    public static void Play(Component target, Vector3 point, float damage, HitElement element,
        Vector3 pushDirection = default, float pushStrength = 0f)
    {
        if (!Enabled || target == null) return;
        var r = GetRunner();
        if (r == null) return;

        Transform root = FindRoot(target);
        bool isBoss = root.GetComponentInParent<BossStateManager>() != null;

        SpawnNumber(r, point, damage, element);
        SpawnBurst(point, element, isBoss);

        // 보스는 밀리지 않고 살짝 움찔만
        if (!isBoss && pushStrength > 0f && pushDirection.sqrMagnitude > 0.0001f)
            r.StartCoroutine(Push(root, pushDirection.normalized, pushStrength));

        r.StartCoroutine(Squash(root, isBoss ? 0.05f : 0.12f));
    }

    // ───────────── 루트/러너 ─────────────
    static Transform FindRoot(Component c)
    {
        var dmg = c.GetComponentInParent<IDamageable>() as Component;
        if (dmg != null) return dmg.transform;
        var agent = c.GetComponentInParent<NavMeshAgent>();
        return agent != null ? agent.transform : c.transform;
    }

    static HitFeedbackRunner GetRunner()
    {
        if (runner != null) return runner;
        var go = new GameObject("[HitFeedback]");
        go.hideFlags = HideFlags.HideInHierarchy;
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<HitFeedbackRunner>();
        return runner;
    }


    static Color ColorOf(HitElement e) => e switch
    {
        HitElement.Fire => new Color(1f, 0.45f, 0.1f),
        HitElement.Water => new Color(0.3f, 0.75f, 1f),
        HitElement.Earth => new Color(0.75f, 0.55f, 0.3f),
        HitElement.Thunder => new Color(1f, 0.95f, 0.35f),
        _ => new Color(1f, 1f, 1f),
    };

    // ───────────── 데미지 숫자 ─────────────
    static void SpawnNumber(HitFeedbackRunner r, Vector3 point, float damage, HitElement element)
    {
        var go = new GameObject("DamageNumber");
        var tmp = go.AddComponent<TextMeshPro>();
        if (tmp.font == null) { Object.Destroy(go); return; }

        int value = Mathf.Max(1, Mathf.RoundToInt(damage));
        bool big = value >= 30;
        tmp.text = value.ToString();
        tmp.fontSize = big ? 7f : 5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = ColorOf(element);
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 200);
        tmp.isOverlay = true; // 벽/적에 가려지지 않게
        tmp.rectTransform.sizeDelta = new Vector2(4f, 2f);

        Vector3 jitter = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0f, 0.3f), Random.Range(-0.35f, 0.35f));
        go.transform.position = point + Vector3.up * 0.6f + jitter;
        r.StartCoroutine(AnimateNumber(go.transform, tmp, big));
    }

    static IEnumerator AnimateNumber(Transform t, TextMeshPro tmp, bool big)
    {
        const float life = 0.85f;
        float time = 0f;
        Vector3 start = t.position;
        Color baseColor = tmp.color;
        float baseScale = big ? 1.15f : 1f;

        while (time < life && t != null)
        {
            time += Time.unscaledDeltaTime;
            float k = time / life;
            var cam = Camera.main;
            if (cam != null) t.rotation = Quaternion.LookRotation(t.position - cam.transform.position);

            // 톡 튀어나왔다가 위로 떠오르며 사라짐
            float pop = k < 0.15f ? Mathf.Lerp(0.4f, 1.35f, k / 0.15f) : Mathf.Lerp(1.35f, 1f, Mathf.Clamp01((k - 0.15f) / 0.2f));
            t.localScale = Vector3.one * pop * baseScale;
            t.position = start + Vector3.up * (1.2f * (1f - (1f - k) * (1f - k)));
            tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
            yield return null;
        }
        if (t != null) Object.Destroy(t.gameObject);
    }

    // ───────────── 속성별 파편 ─────────────
    static Material GetParticleMaterial()
    {
        if (particleMaterial != null) return particleMaterial;
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;
        particleMaterial = new Material(shader);
        // 가산 혼합으로 빛나는 느낌
        if (particleMaterial.HasProperty("_Surface"))
        {
            particleMaterial.SetFloat("_Surface", 1f);
            particleMaterial.SetFloat("_Blend", 2f);
            particleMaterial.SetOverrideTag("RenderType", "Transparent");
            particleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            particleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            particleMaterial.SetInt("_ZWrite", 0);
            particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            particleMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        return particleMaterial;
    }

    static void SpawnBurst(Vector3 point, HitElement element, bool isBoss)
    {
        var mat = GetParticleMaterial();
        if (mat == null) return;

        var go = new GameObject("HitBurst_" + element);
        go.transform.position = point;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = true; // 히트스톱 중에도 터지게
        main.startColor = new ParticleSystem.MinMaxGradient(ColorOf(element), Color.Lerp(ColorOf(element), Color.white, 0.6f));

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        int count = isBoss ? 22 : 16;
        switch (element)
        {
            case HitElement.Fire:
                main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
                main.gravityModifier = -0.6f; // 불꽃은 위로
                break;
            case HitElement.Water:
                main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
                main.gravityModifier = 1.5f; // 물보라는 떨어짐
                count = isBoss ? 14 : 10;
                break;
            case HitElement.Earth:
                main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
                main.gravityModifier = 2.5f; // 돌 파편
                break;
            case HitElement.Thunder:
                main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 12f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                main.gravityModifier = 0f;
                count = isBoss ? 30 : 24;
                break;
            default:
                main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
                main.gravityModifier = 0.8f;
                break;
        }

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        if (element == HitElement.Thunder || element == HitElement.Physical)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch; // 번쩍이는 줄무늬
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 1.5f;
        }

        ps.Emit(count);
        Object.Destroy(go, 1.2f);
    }

    // ───────────── 밀려남 / 움찔 ─────────────
    static IEnumerator Push(Transform root, Vector3 dir, float strength)
    {
        dir.y = 0f;
        var agent = root != null ? root.GetComponent<NavMeshAgent>() : null;
        float speed = strength;
        const float duration = 0.18f;
        float t = 0f;

        while (t < duration && root != null)
        {
            float dt = Time.deltaTime;
            t += dt;
            Vector3 step = dir * (speed * dt);
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.Move(step);
            else root.position += step;
            speed = Mathf.Lerp(strength, 0f, t / duration);
            yield return null;
        }
    }

    static IEnumerator Squash(Transform root, float amount)
    {
        if (root == null) yield break;
        // 루트를 늘리면 NavMeshAgent 크기도 바뀌므로 애니메이터(외형) 쪽을 움찔시킴
        var anim = root.GetComponentInChildren<Animator>();
        Transform visual = anim != null && anim.transform != root ? anim.transform : null;
        if (visual == null) yield break;

        // 연속 타격으로 크기가 누적되지 않게 기준 크기 기억
        if (!squashState.TryGetValue(visual, out var st)) st = (visual.localScale, 0);
        int token = st.token + 1;
        Vector3 baseScale = st.baseScale;
        squashState[visual] = (baseScale, token);
        bool Current() => squashState.TryGetValue(visual, out var c) && c.token == token;

        const float duration = 0.16f;
        float t = 0f;
        while (t < duration && visual != null && Current())
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;
            float s = Mathf.Sin(k * Mathf.PI) * amount;
            visual.localScale = new Vector3(baseScale.x * (1f + s), baseScale.y * (1f - s * 0.8f), baseScale.z * (1f + s));
            yield return null;
        }
        if (visual != null && Current()) { visual.localScale = baseScale; squashState.Remove(visual); }
    }

}
