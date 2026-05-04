using System.Collections;
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

    private Dictionary<string, AudioClip> bgmClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();

    [Header("Volumes")]
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("3D SFX Pool")]
    public int poolInitialSize = 8;
    public float minDistance = 1f;
    public float maxDistance = 30f;

    private readonly Queue<AudioSource> pool3D = new Queue<AudioSource>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildBanks();
        Prepare3DPool();
        ApplyVolumeToSources();
    }

    private void BuildBanks()
    {
        bgmClips.Clear();
        sfxClips.Clear();

        if (bgmClipList != null)
        {
            foreach (var it in bgmClipList)
            {
                if (!string.IsNullOrEmpty(it.name) && it.clip != null && !bgmClips.ContainsKey(it.name))
                    bgmClips.Add(it.name, it.clip);
            }
        }

        if (sfxClipList != null)
        {
            foreach (var it in sfxClipList)
            {
                if (!string.IsNullOrEmpty(it.name) && it.clip != null && !sfxClips.ContainsKey(it.name))
                    sfxClips.Add(it.name, it.clip);
            }
        }
    }

    private void Prepare3DPool()
    {
        for (int i = 0; i < poolInitialSize; i++)
            pool3D.Enqueue(Create3DAudioSource());
    }

    private AudioSource Create3DAudioSource()
    {
        GameObject go = new GameObject("SFX3D");
        go.transform.SetParent(transform);

        AudioSource src = go.AddComponent<AudioSource>();
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.playOnAwake = false;

        return src;
    }

    private AudioSource Get3DSource(Vector3 pos, float pitch)
    {
        AudioSource src = pool3D.Count > 0 ? pool3D.Dequeue() : Create3DAudioSource();

        src.transform.position = pos;
        src.pitch = pitch;
        src.volume = sfxVolume;

        return src;
    }

    private void Return3DSource(AudioSource src)
    {
        if (src == null) return;

        src.Stop();
        src.clip = null;
        src.transform.SetParent(transform);

        pool3D.Enqueue(src);
    }

    public bool HasSFX(string name)
    {
        return !string.IsNullOrEmpty(name) && sfxClips.ContainsKey(name);
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyVolumeToSources();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumeToSources();
    }

    public void SetVolumes(float bgm, float sfx)
    {
        bgmVolume = Mathf.Clamp01(bgm);
        sfxVolume = Mathf.Clamp01(sfx);

        ApplyVolumeToSources();
    }

    public void ApplyVolumeToSources()
    {
        if (bgm2DSource != null)
            bgm2DSource.volume = bgmVolume;

        if (sfx2DSource != null)
            sfx2DSource.volume = sfxVolume;
    }

    public void PlayBGM(string name, bool loop = true)
    {
        if (bgm2DSource == null) return;
        if (string.IsNullOrEmpty(name)) return;
        if (!bgmClips.TryGetValue(name, out AudioClip clip)) return;

        bgm2DSource.loop = loop;
        bgm2DSource.clip = clip;
        bgm2DSource.volume = bgmVolume;
        bgm2DSource.Play();
    }

    public void StopBGM()
    {
        if (bgm2DSource != null)
            bgm2DSource.Stop();
    }

    public void PlaySFX2D(string name, float volume = 1f, float pitch = 1f)
    {
        if (sfx2DSource == null) return;
        if (string.IsNullOrEmpty(name)) return;
        if (!sfxClips.TryGetValue(name, out AudioClip clip)) return;

        sfx2DSource.pitch = pitch;
        sfx2DSource.PlayOneShot(clip, sfxVolume * volume);
    }

    public void PlaySFX2D(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (sfx2DSource == null || clip == null) return;

        sfx2DSource.pitch = pitch;
        sfx2DSource.PlayOneShot(clip, sfxVolume * volume);
    }

    public void PlaySFX3D(string name, Vector3 pos, float volume = 1f, float pitch = 1f)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (!sfxClips.TryGetValue(name, out AudioClip clip)) return;

        PlaySFX3D(clip, pos, volume, pitch);
    }

    public void PlaySFX3D(AudioClip clip, Vector3 pos, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource src = Get3DSource(pos, pitch);

        src.clip = clip;
        src.volume = sfxVolume * volume;
        src.Play();

        StartCoroutine(ReturnWhenFinished(src, clip.length / Mathf.Max(0.01f, src.pitch)));
    }

    private IEnumerator ReturnWhenFinished(AudioSource src, float t)
    {
        yield return new WaitForSecondsRealtime(t);

        Return3DSource(src);
    }
}