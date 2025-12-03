using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("2D Sources")]
    public AudioSource bgm2DSource;
    public AudioSource sfx2DSource;

    [System.Serializable] public struct NamedClip { public string name; public AudioClip clip; }
    [Header("SFX Bank")]
    public NamedClip[] sfxClipList;

    Dictionary<string, AudioClip> sfxBank = new Dictionary<string, AudioClip>();

    [Header("3D SFX Pool")]
    public int poolInitialSize = 6;
    public float minDistance = 1f;
    public float maxDistance = 30f;

    Queue<AudioSource> pool = new Queue<AudioSource>();
    Transform poolRoot;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var nc in sfxClipList)
            if (nc.clip && !sfxBank.ContainsKey(nc.name))
                sfxBank.Add(nc.name, nc.clip);

        poolRoot = new GameObject("SFX3D_Pool").transform;
        DontDestroyOnLoad(poolRoot.gameObject);
        for (int i = 0; i < poolInitialSize; i++) pool.Enqueue(Create3DSource());
    }

    AudioSource Create3DSource()
    {
        var go = new GameObject("SFX3D");
        go.transform.SetParent(poolRoot);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        go.SetActive(false);
        return src;
    }

    public void PlaySFX2D(string name, float vol = 1f, float pitch = 1f)
    { if (sfxBank.TryGetValue(name, out var clip)) PlaySFX2D(clip, vol, pitch); }

    public void PlaySFX2D(AudioClip clip, float vol = 1f, float pitch = 1f)
    {
        if (!clip || !sfx2DSource) return;
        sfx2DSource.pitch = pitch;
        sfx2DSource.PlayOneShot(clip, vol);
    }

    public void PlaySFX3D(string name, Vector3 pos, float vol = 1f, float pitch = 1f)
    { if (sfxBank.TryGetValue(name, out var clip)) PlaySFX3D(clip, pos, vol, pitch); }

    public void PlaySFX3D(AudioClip clip, Vector3 pos, float vol = 1f, float pitch = 1f)
    {
        if (!clip) return;
        var src = pool.Count > 0 ? pool.Dequeue() : Create3DSource();
        src.transform.position = pos;
        src.clip = clip;
        src.volume = vol;
        src.pitch = pitch;
        src.gameObject.SetActive(true);
        src.Play();
        StartCoroutine(ReturnToPool(src, clip.length / Mathf.Max(0.01f, pitch)));
    }

    IEnumerator ReturnToPool(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        src.Stop(); src.clip = null; src.gameObject.SetActive(false);
        pool.Enqueue(src);
    }
}