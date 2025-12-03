using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ParticleManager : MonoBehaviour
{
    public static ParticleManager instance { get; private set; }

    public enum ParticleType
    {
        FireCast,
        FireHit
    }

    [Header("Prefabs")]
    public GameObject fireCastVFX;   // 시전(발사)
    public GameObject fireHitVFX;    // 히트

    [Header("Pool")]
    public int poolSize = 20;
    public bool expandIfEmpty = true;
    public float defaultLifetime = 1.2f;

    class PooledFX
    {
        public GameObject go;
        public ParticleSystem[] systems;
        public float estimatedLife; // loop 있으면 0
    }

    readonly Dictionary<ParticleType, GameObject> prefabMap = new();
    readonly Dictionary<ParticleType, Queue<PooledFX>> pools = new();
    Transform poolRoot;

    void Awake()
    {
        if (instance && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        if (fireCastVFX) prefabMap[ParticleType.FireCast] = fireCastVFX;
        if (fireHitVFX) prefabMap[ParticleType.FireHit] = fireHitVFX;

        poolRoot = new GameObject("VFX_Pool").transform;
        poolRoot.SetParent(transform);

        foreach (var kv in prefabMap)
        {
            var q = new Queue<PooledFX>();
            for (int i = 0; i < poolSize; i++) q.Enqueue(CreateOne(kv.Key));
            pools[kv.Key] = q;
        }
    }

    PooledFX CreateOne(ParticleType type)
    {
        var prefab = prefabMap.TryGetValue(type, out var p) ? p : null;
        if (!prefab) { Debug.LogWarning($"[ParticleManager] Prefab missing for {type}"); return null; }

        var go = Instantiate(prefab, poolRoot);
        go.SetActive(false);

        var systems = go.GetComponentsInChildren<ParticleSystem>(true);

        float maxLife = 0f; bool loop = false;
        foreach (var ps in systems)
        {
            var m = ps.main;
            if (m.loop) loop = true;
            float life = m.duration + Estimate(m.startLifetime);
            if (life > maxLife) maxLife = life;
        }

        return new PooledFX
        {
            go = go,
            systems = systems,
            estimatedLife = loop ? 0f : Mathf.Max(maxLife, 0.01f)
        };
    }

    static float Estimate(ParticleSystem.MinMaxCurve c)
    {
        return c.mode switch
        {
            ParticleSystemCurveMode.Constant => c.constant,
            ParticleSystemCurveMode.TwoConstants => c.constantMax,
            ParticleSystemCurveMode.Curve => c.curve.length > 0 ? c.curve[c.curve.length - 1].time : 0f,
            ParticleSystemCurveMode.TwoCurves => c.curveMax.length > 0 ? c.curveMax[c.curveMax.length - 1].time : 0f,
            _ => 0f
        };
    }

    PooledFX Get(ParticleType type)
    {
        if (!pools.TryGetValue(type, out var q))
            pools[type] = q = new Queue<PooledFX>();

        while (q.Count > 0)
        {
            var fx = q.Dequeue();
            if (fx != null && fx.go != null) return fx;
        }

        return expandIfEmpty ? CreateOne(type) : null;
    }

    void Return(ParticleType type, PooledFX fx)
    {
        if (fx == null || fx.go == null) return;

        foreach (var ps in fx.systems)
        {
            if (!ps) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
        }
        fx.go.transform.SetParent(poolRoot, false);
        fx.go.SetActive(false);
        pools[type].Enqueue(fx);
    }

    IEnumerator ReturnAfter(ParticleType type, PooledFX fx, float sec)
    {
        yield return new WaitForSeconds(sec);
        Return(type, fx);
    }

    // === API ===
    public void PlayParticle(ParticleType type, Vector3 position)
        => PlayParticle(type, position, Quaternion.identity, null, -1f);

    public void PlayParticle(ParticleType type, Vector3 position, Quaternion rotation, float lifetimeOverride = -1f)
        => PlayParticle(type, position, rotation, null, lifetimeOverride);

    public void PlayParticleAttached(ParticleType type, Transform parent, Vector3 localPos, Quaternion localRot, float lifetimeOverride = -1f)
    {
        var fx = Get(type);
        if (fx == null) return;

        var t = fx.go.transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        t.localRotation = localRot;

        ActivateAndSchedule(type, fx, lifetimeOverride);
    }

    void PlayParticle(ParticleType type, Vector3 position, Quaternion rotation, Transform parent, float lifetimeOverride)
    {
        var fx = Get(type);
        if (fx == null) return;

        var t = fx.go.transform;
        t.SetParent(parent ? parent : poolRoot, false);
        t.position = position;
        t.rotation = rotation;

        ActivateAndSchedule(type, fx, lifetimeOverride);
    }

    void ActivateAndSchedule(ParticleType type, PooledFX fx, float lifetimeOverride)
    {
        foreach (var ps in fx.systems)
        {
            if (!ps) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
        }

        fx.go.SetActive(true);
        foreach (var ps in fx.systems) if (ps) ps.Play();

        float life = lifetimeOverride > 0f
            ? lifetimeOverride
            : (fx.estimatedLife > 0f ? fx.estimatedLife : defaultLifetime);

        StartCoroutine(ReturnAfter(type, fx, life + 0.05f));
    }
}