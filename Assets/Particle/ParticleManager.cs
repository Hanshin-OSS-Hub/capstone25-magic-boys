using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ParticleType { FireCast, FireHit, SparkCast, SparkHit }

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject fireCastVFX, fireHitVFX, sparkCastVFX, sparkHitVFX;

    [Header("Pool")]
    public int poolSize = 20;
    public bool expandIfEmpty = true;
    public float defaultLifetime = 3f;

    readonly Dictionary<ParticleType, Queue<GameObject>> pools = new();
    readonly Dictionary<ParticleType, GameObject> prefabs = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        prefabs[ParticleType.FireCast] = fireCastVFX;
        prefabs[ParticleType.FireHit] = fireHitVFX;
        prefabs[ParticleType.SparkCast] = sparkCastVFX;
        prefabs[ParticleType.SparkHit] = sparkHitVFX;

        foreach (var kv in prefabs) CreatePool(kv.Key, kv.Value, poolSize);
    }

    void CreatePool(ParticleType t, GameObject prefab, int count)
    {
        var q = new Queue<GameObject>();
        pools[t] = q;
        if (!prefab) return;
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            q.Enqueue(go);
        }
    }

    GameObject Get(ParticleType t)
    {
        if (!pools.TryGetValue(t, out var q)) { q = new Queue<GameObject>(); pools[t] = q; }
        if (q.Count > 0) return q.Dequeue();
        if (!prefabs.TryGetValue(t, out var prefab) || !prefab || !expandIfEmpty) return null;
        var go = Instantiate(prefab, transform);
        go.SetActive(false);
        return go;
    }

    public void Play(ParticleType t, Vector3 pos, Quaternion rot, float? lifetimeOverride = null)
    {
        var go = Get(t);
        if (!go) return;
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);

        var ps = go.GetComponentInChildren<ParticleSystem>();
        if (ps) { ps.Clear(); ps.Play(); StartCoroutine(ReturnWhenDone(t, go, ps, lifetimeOverride)); }
        else { StartCoroutine(ReturnAfter(t, go, lifetimeOverride ?? defaultLifetime)); }
    }

    IEnumerator ReturnWhenDone(ParticleType t, GameObject go, ParticleSystem ps, float? life)
    {
        if (life.HasValue) yield return new WaitForSeconds(life.Value);
        else yield return new WaitWhile(() => ps.IsAlive(true));
        go.SetActive(false);
        pools[t].Enqueue(go);
    }
    IEnumerator ReturnAfter(ParticleType t, GameObject go, float sec)
    {
        yield return new WaitForSeconds(sec);
        go.SetActive(false);
        pools[t].Enqueue(go);
    }
}