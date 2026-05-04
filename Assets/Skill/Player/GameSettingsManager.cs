using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-400)]
public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    private const string KEY_MASTER = "SETTING_MASTER_VOLUME";
    private const string KEY_BGM = "SETTING_BGM_VOLUME";
    private const string KEY_SFX = "SETTING_SFX_VOLUME";
    private const string KEY_WIDTH = "SETTING_SCREEN_WIDTH";
    private const string KEY_HEIGHT = "SETTING_SCREEN_HEIGHT";
    private const string KEY_FULLSCREEN = "SETTING_FULLSCREEN";

    public float MasterVolume => PlayerPrefs.GetFloat(KEY_MASTER, 1f);
    public float BGMVolume => PlayerPrefs.GetFloat(KEY_BGM, 1f);
    public float SFXVolume => PlayerPrefs.GetFloat(KEY_SFX, 1f);

    public int ScreenWidth => PlayerPrefs.GetInt(KEY_WIDTH, Screen.width);
    public int ScreenHeight => PlayerPrefs.GetInt(KEY_HEIGHT, Screen.height);
    public bool IsFullscreen => PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyAllSettings();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyAllSettings();
    }

    public void ApplyAllSettings()
    {
        ApplyMasterVolume(MasterVolume);
        ApplySoundManagerVolume();

        int width = ScreenWidth;
        int height = ScreenHeight;
        bool fullscreen = IsFullscreen;

        if (width > 0 && height > 0)
            Screen.SetResolution(width, height, fullscreen);
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(KEY_MASTER, value);
        PlayerPrefs.Save();

        ApplyMasterVolume(value);
    }

    public void SetBGMVolume(float value)
    {
        value = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(KEY_BGM, value);
        PlayerPrefs.Save();

        ApplySoundManagerVolume();
    }

    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(KEY_SFX, value);
        PlayerPrefs.Save();

        ApplySoundManagerVolume();
    }

    public void SetResolution(int width, int height, bool fullscreen)
    {
        PlayerPrefs.SetInt(KEY_WIDTH, width);
        PlayerPrefs.SetInt(KEY_HEIGHT, height);
        PlayerPrefs.SetInt(KEY_FULLSCREEN, fullscreen ? 1 : 0);
        PlayerPrefs.Save();

        Screen.SetResolution(width, height, fullscreen);
    }

    private void ApplyMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    private void ApplySoundManagerVolume()
    {
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.SetVolumes(BGMVolume, SFXVolume);
    }
}