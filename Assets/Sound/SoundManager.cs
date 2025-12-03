using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("2D Sources")]
    public AudioSource bgm2DSource;   // BGM(2D, loop ON)
    public AudioSource sfx2DSource;   // SFX(2D, one-shot)

    [Serializable] public struct NamedAudioClip { public string name; public AudioClip clip; }

    [Header("Banks (optional)")]          // ✅ Header는 '필드' 위에
    public NamedAudioClip[] bgmClipList;   // 이름→클립 매핑용
    public NamedAudioClip[] sfxClipList;

    Dictionary<string, AudioClip> _bgm = new();
    Dictionary<string, AudioClip> _sfx = new();

    [Header("Volumes")]
    [Range(0, 1)] public float bgmVolume = 1f;
    [Range(0, 1)] public float sfxVolume = 1f;

    [Header("3D SFX Pool")]
    public int poolInitialSize = 6;
    public float minDistance = 1f;
    public float maxDistance = 30f;
    Queue<AudioSource> pool = new();
    Transform poolRoot;

    AudioSource bgm3DSource;            // 지역 BGM용(선택)
    Transform bgm3DFollowTarget;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var e in bgmClipList) if (e.clip && !string.IsNullOrEmpty(e.name)) _bgm[e.name] = e.clip;
        foreach (var e in sfxClipList) if (e.clip && !string.IsNullOrEmpty(e.name)) _sfx[e.name] = e.clip;

        if (!bgm2DSource) bgm2DSource = gameObject.AddComponent<AudioSource>();
        if (!sfx2DSource) sfx2DSource = gameObject.AddComponent<AudioSource>();
        bgm2DSource.playOnAwake = false; bgm2DSource.loop = true; bgm2DSource.spatialBlend = 0f;
        sfx2DSource.playOnAwake = false; sfx2DSource.loop = false; sfx2DSource.spatialBlend = 0f;

        ApplyVolumes();

        poolRoot = new GameObject("SFX3D_Pool").transform;
        poolRoot.SetParent(transform);
        for (int i = 0; i < poolInitialSize; i++) pool.Enqueue(CreateEmitter());
    }

    void OnValidate() => ApplyVolumes();
    void Update()
    {
        if (bgm3DSource && bgm3DFollowTarget)
            bgm3DSource.transform.position = bgm3DFollowTarget.position;
    }

    void ApplyVolumes()
    {
        if (bgm2DSource) bgm2DSource.volume = bgmVolume;
        if (sfx2DSource) sfx2DSource.volume = sfxVolume;
        if (bgm3DSource) bgm3DSource.volume = bgmVolume;
    }

    AudioSource CreateEmitter()
    {
        var go = new GameObject("SFX3D");
        go.transform.SetParent(poolRoot);
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false; a.loop = false;
        a.spatialBlend = 1f;
        a.rolloffMode = AudioRolloffMode.Logarithmic;
        a.minDistance = minDistance; a.maxDistance = maxDistance;
        go.SetActive(false);
        return a;
    }
    AudioSource GetEmitter()
    {
        var a = pool.Count > 0 ? pool.Dequeue() : CreateEmitter();
        a.gameObject.SetActive(true);
        return a;
    }
    IEnumerator ReturnAfter(AudioSource a, float sec)
    {
        yield return new WaitForSeconds(sec);
        a.Stop(); a.gameObject.SetActive(false); pool.Enqueue(a);
    }

    // --------- BGM 2D ----------
    public void PlayBGM2D(string name, float? volume = null)
    {
        if (!_bgm.TryGetValue(name, out var clip) || !clip) return;
        PlayBGM2D(clip, volume);
    }
    public void PlayBGM2D(AudioClip clip, float? volume = null)
    {
        if (!clip) return;
        bgm2DSource.clip = clip;
        if (volume.HasValue) bgmVolume = Mathf.Clamp01(volume.Value);
        ApplyVolumes();
        bgm2DSource.Play();
    }
    public void StopBGM2D() => bgm2DSource.Stop();

    // --------- BGM 3D (optional) ----------
    public void PlayBGM3D(string name, Transform follow = null, float? volume = null)
    {
        if (!_bgm.TryGetValue(name, out var clip) || !clip) return;
        PlayBGM3D(clip, follow, volume);
    }
    public void PlayBGM3D(AudioClip clip, Transform follow = null, float? volume = null)
    {
        if (!clip) return;
        if (!bgm3DSource)
        {
            var go = new GameObject("BGM3D");
            go.transform.SetParent(transform);
            bgm3DSource = go.AddComponent<AudioSource>();
            bgm3DSource.playOnAwake = false; bgm3DSource.loop = true;
            bgm3DSource.spatialBlend = 1f;
            bgm3DSource.rolloffMode = AudioRolloffMode.Logarithmic;
            bgm3DSource.minDistance = minDistance; bgm3DSource.maxDistance = maxDistance;
        }
        if (volume.HasValue) bgmVolume = Mathf.Clamp01(volume.Value);
        bgm3DSource.clip = clip; ApplyVolumes();

        bgm3DFollowTarget = follow;
        if (follow) { bgm3DSource.transform.position = follow.position; bgm3DSource.transform.SetParent(follow); }
        else { bgm3DSource.transform.SetParent(transform); }

        bgm3DSource.Play();
    }
    public void StopBGM3D() { if (bgm3DSource) bgm3DSource.Stop(); }

    // --------- SFX ----------
    public void PlaySFX2D(string name, float volume = 1f)
    {
        if (!_sfx.TryGetValue(name, out var clip) || !clip) return;
        PlaySFX2D(clip, volume);
    }
    public void PlaySFX2D(AudioClip clip, float volume = 1f)
    {
        if (!clip) return;
        sfx2DSource.PlayOneShot(clip, Mathf.Clamp01(volume) * sfxVolume);
    }

    public void PlaySFX3D(string name, Vector3 pos, float volume = 1f, float pitch = 1f)
    {
        if (!_sfx.TryGetValue(name, out var clip) || !clip) return;
        PlaySFX3D(clip, pos, volume, pitch);
    }
    public void PlaySFX3D(AudioClip clip, Vector3 pos, float volume = 1f, float pitch = 1f)
    {
        if (!clip) return;
        var a = GetEmitter();
        a.transform.position = pos;
        a.volume = Mathf.Clamp01(volume) * sfxVolume;
        a.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        a.clip = clip;
        a.Play();
        StartCoroutine(ReturnAfter(a, clip.length / Mathf.Max(0.01f, a.pitch) + 0.05f));
    }

    public void SetBGMVolume(float v) { bgmVolume = Mathf.Clamp01(v); ApplyVolumes(); }
    public void SetSFXVolume(float v) { sfxVolume = Mathf.Clamp01(v); ApplyVolumes(); }
    public void StopAllSFX()
    {
        foreach (var a in pool) { a.Stop(); a.gameObject.SetActive(false); }
        foreach (Transform t in poolRoot)
        {
            var a = t.GetComponent<AudioSource>();
            if (a && t.gameObject.activeSelf) { a.Stop(); t.gameObject.SetActive(false); pool.Enqueue(a); }
        }
    }
}