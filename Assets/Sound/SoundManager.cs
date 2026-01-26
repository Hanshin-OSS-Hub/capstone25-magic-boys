using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("2D Sources")]
    public AudioSource bgm2DSource;
    public AudioSource sfx2DSource;

    [System.Serializable]
    public struct NamedAudioClip
    {
        public string name;
        public AudioClip clip;
    }

    [Header("Banks")]
    public NamedAudioClip[] bgmClipList;
    public NamedAudioClip[] sfxClipList;

    Dictionary<string, AudioClip> bgmClips = new();
    Dictionary<string, AudioClip> sfxClips = new();

    [Header("Volumes")]
    [Range(0, 1)] public float bgmVolume = 1f;
    [Range(0, 1)] public float sfxVolume = 1f;

    [Header("3D SFX Pool")]
    public int poolInitialSize = 8;
    public float minDistance = 1f;
    public float maxDistance = 30f;
    readonly Queue<AudioSource> pool3D = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var it in bgmClipList) if (it.clip && !bgmClips.ContainsKey(it.name)) bgmClips.Add(it.name, it.clip);
        foreach (var it in sfxClipList) if (it.clip && !sfxClips.ContainsKey(it.name)) sfxClips.Add(it.name, it.clip);

        // 3D 풀 준비
        for (int i = 0; i < poolInitialSize; i++) pool3D.Enqueue(Create3DAudioSource());
    }

    AudioSource Create3DAudioSource()
    {
        var go = new GameObject("SFX3D");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.playOnAwake = false;
        return src;
    }

    AudioSource Get3DSource(Vector3 pos, float pitch)
    {
        var src = pool3D.Count > 0 ? pool3D.Dequeue() : Create3DAudioSource();
        src.transform.position = pos;
        src.pitch = pitch;
        src.volume = sfxVolume;
        return src;
    }
    void Return3DSource(AudioSource src)
    {
        if (!src) return;
        src.clip = null;
        pool3D.Enqueue(src);
    }

    public bool HasSFX(string name) => sfxClips.ContainsKey(name);

    // ---- BGM ----
    public void PlayBGM(string name, bool loop = true)
    {
        if (!bgm2DSource || !bgmClips.TryGetValue(name, out var clip)) return;
        bgm2DSource.loop = loop;
        bgm2DSource.clip = clip;
        bgm2DSource.volume = bgmVolume;
        bgm2DSource.Play();
    }
    public void StopBGM() { if (bgm2DSource) bgm2DSource.Stop(); }

    // ---- SFX 2D ----
    public void PlaySFX2D(string name, float volume = 1f, float pitch = 1f)
    {
        if (!sfx2DSource || !sfxClips.TryGetValue(name, out var clip)) return;
        sfx2DSource.pitch = pitch;
        sfx2DSource.PlayOneShot(clip, sfxVolume * volume);
    }
    public void PlaySFX2D(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (!sfx2DSource || !clip) return;
        sfx2DSource.pitch = pitch;
        sfx2DSource.PlayOneShot(clip, sfxVolume * volume);
    }

    // ---- SFX 3D ----
    public void PlaySFX3D(string name, Vector3 pos, float volume = 1f, float pitch = 1f)
    {
        if (!sfxClips.TryGetValue(name, out var clip)) return;
        PlaySFX3D(clip, pos, volume, pitch);
    }
    public void PlaySFX3D(AudioClip clip, Vector3 pos, float volume = 1f, float pitch = 1f)
    {
        if (!clip) return;
        var src = Get3DSource(pos, pitch);
        src.clip = clip;
        src.volume = sfxVolume * volume;
        src.Play();
        StartCoroutine(ReturnWhenFinished(src, clip.length / Mathf.Max(0.01f, src.pitch)));
    }
    System.Collections.IEnumerator ReturnWhenFinished(AudioSource src, float t)
    {
        yield return new WaitForSeconds(t);
        Return3DSource(src);
    }
}