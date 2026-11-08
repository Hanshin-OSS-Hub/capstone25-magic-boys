using System.Collections;
using UnityEngine;

// 연출 공용 도구: 프리팹 이펙트 재생, 코드로 만드는 파티클(먼지/증기/불씨), 빛 번쩍임, 화면 흔들림·섬광·슬로모션.
public static class VfxKit
{
    private static Material additiveMat, alphaMat;
    private static float flashAlpha;
    private static Color flashColor;
    private static bool slowMoRunning;

    // ───────────── 프리팹 이펙트 ─────────────
    /// <summary>프리팹 이펙트 재생. timeOffset만큼 시작 지연을 당겨서 바로 터지게 할 수 있음. hideNames에 포함된 자식은 끔.</summary>
    public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, float life,
        float timeOffset = 0f, string[] hideNames = null, Transform parent = null)
    {
        if (prefab == null) return null;
        var go = Object.Instantiate(prefab, pos, rot, parent);
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (hideNames != null)
            {
                foreach (var n in hideNames)
                    if (ps.name.Contains(n)) { ps.gameObject.SetActive(false); break; }
            }
            if (timeOffset > 0f && ps.gameObject.activeSelf)
            {
                var main = ps.main;
                var d = main.startDelay;
                main.startDelay = new ParticleSystem.MinMaxCurve(Mathf.Max(0f, d.constantMin - timeOffset), Mathf.Max(0f, d.constantMax - timeOffset));
            }
        }
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(false)) ps.Play(false);
        if (life > 0f) Object.Destroy(go, life);
        return go;
    }

    // ───────────── 코드 파티클 ─────────────
    static Material GetMat(bool additive)
    {
        ref Material m = ref additive ? ref additiveMat : ref alphaMat;
        if (m != null) return m;
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;
        m = new Material(shader);
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        // 부드러운 원형 입자 텍스처
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 15.5f;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
            }
        tex.Apply();
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        m.mainTexture = tex;
        return m;
    }

    public struct BurstSettings
    {
        public Color colorA, colorB;
        public int count;
        public Vector2 speed, size, life;
        public float gravity, radius;
        public bool additive, upward, stretch;
    }

    public static ParticleSystem CreateSystem(string name, Vector3 pos, BurstSettings s, Transform parent = null)
    {
        var mat = GetMat(s.additive);
        if (mat == null) return null;
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(s.colorA, s.colorB);
        main.startSpeed = new ParticleSystem.MinMaxCurve(s.speed.x, s.speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(s.size.x, s.size.y);
        main.startLifetime = new ParticleSystem.MinMaxCurve(s.life.x, s.life.y);
        main.gravityModifier = s.gravity;
        main.maxParticles = 2000;
        var em = ps.emission; em.rateOverTime = 0f;
        var shape = ps.shape;
        shape.shapeType = s.upward ? ParticleSystemShapeType.Cone : ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.01f, s.radius);
        if (s.upward) { shape.angle = 35f; go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); }
        var sol = ps.sizeOverLifetime; sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1, s.additive ? 0f : 1.6f)));
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        if (s.stretch) { r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 1.5f; }
        return ps;
    }

    public static void Burst(Vector3 pos, BurstSettings s, float destroyAfter = 3f)
    {
        var ps = CreateSystem("Burst", pos, s);
        if (ps == null) return;
        ps.Emit(s.count);
        Object.Destroy(ps.gameObject, destroyAfter);
    }

    public static BurstSettings Dust(int count = 30, float radius = 1f) => new BurstSettings
    {
        colorA = new Color(0.45f, 0.38f, 0.32f, 0.7f), colorB = new Color(0.3f, 0.26f, 0.22f, 0.5f),
        count = count, speed = new Vector2(1.5f, 5f), size = new Vector2(0.8f, 1.8f), life = new Vector2(0.8f, 1.6f),
        gravity = -0.05f, radius = radius, additive = false
    };

    public static BurstSettings Embers(int count = 40, float radius = 1f) => new BurstSettings
    {
        colorA = new Color(1f, 0.55f, 0.1f), colorB = new Color(1f, 0.2f, 0.02f),
        count = count, speed = new Vector2(3f, 9f), size = new Vector2(0.06f, 0.18f), life = new Vector2(0.5f, 1.4f),
        gravity = 0.8f, radius = radius, additive = true, upward = true, stretch = true
    };

    public static BurstSettings Steam(int count = 25, float radius = 0.5f) => new BurstSettings
    {
        colorA = new Color(0.85f, 0.85f, 0.85f, 0.55f), colorB = new Color(0.6f, 0.6f, 0.65f, 0.4f),
        count = count, speed = new Vector2(3f, 7f), size = new Vector2(0.6f, 1.4f), life = new Vector2(0.7f, 1.3f),
        gravity = -0.3f, radius = radius, additive = false, upward = true
    };

    /// <summary>바닥을 따라 퍼지는 충격파 링 (먼지)</summary>
    public static void Shockwave(Vector3 pos, float radius, Color color, int count = 70)
    {
        var s = Dust(count, 0.3f);
        s.colorA = color; s.colorB = color * 0.8f;
        s.speed = new Vector2(radius * 1.6f, radius * 2.0f);
        s.life = new Vector2(0.45f, 0.6f);
        s.size = new Vector2(0.9f, 1.6f);
        var ps = CreateSystem("Shockwave", pos + Vector3.up * 0.2f, s);
        if (ps == null) return;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
        ps.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = radius * 0.6f; lim.dampen = 0.25f;
        ps.Emit(count);
        Object.Destroy(ps.gameObject, 2f);
    }

    // ───────────── 빛 / 소리 ─────────────
    public static void LightFlash(Vector3 pos, Color color, float intensity, float range, float duration)
    {
        var go = new GameObject("FlashLight");
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = color; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
        VfxRunner.Instance.StartCoroutine(FadeLight(l, intensity, duration));
    }

    static IEnumerator FadeLight(Light l, float intensity, float duration)
    {
        float t = 0f;
        while (t < duration && l != null)
        {
            t += Time.unscaledDeltaTime;
            l.intensity = intensity * (1f - t / duration) * (1f - t / duration);
            yield return null;
        }
        if (l != null) Object.Destroy(l.gameObject);
    }

    public static void PlayClip(AudioClip clip, Vector3 pos, float volume = 1f)
    {
        if (clip == null) return;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX3D(clip, pos, volume, Random.Range(0.95f, 1.05f));
        else AudioSource.PlayClipAtPoint(clip, pos, volume);
    }

    // ───────────── 화면 연출 ─────────────
    public static void Shake(Vector3 source, float maxAmplitude, float duration, float falloffDistance = 25f)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var shaker = cam.GetComponentInParent<CameraSwitch>();
        if (shaker == null) return;
        float d = Vector3.Distance(cam.transform.position, source);
        float k = Mathf.Clamp01(1f - d / falloffDistance);
        if (k > 0.02f) shaker.Shake(maxAmplitude * k, duration);
    }

    public static void ScreenFlash(Color color, float alpha)
    {
        flashColor = color;
        flashAlpha = Mathf.Max(flashAlpha, alpha);
        VfxRunner.Instance.enabled = true;
    }

    public static void SlowMotion(float scale, float duration)
    {
        if (slowMoRunning || Time.timeScale < 0.5f) return;
        VfxRunner.Instance.StartCoroutine(SlowMo(scale, duration));
    }

    static IEnumerator SlowMo(float scale, float duration)
    {
        slowMoRunning = true;
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(duration);
        if (Mathf.Approximately(Time.timeScale, scale)) Time.timeScale = 1f;
        slowMoRunning = false;
    }

    internal static void DrawFlash()
    {
        if (flashAlpha <= 0.001f) return;
        var prev = GUI.color;
        GUI.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = prev;
        if (Event.current.type == EventType.Repaint)
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 2.5f);
    }
}
