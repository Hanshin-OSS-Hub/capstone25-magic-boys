using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [Header("Sound UI")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Screen UI")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private bool isRefreshing;

    void Awake()
    {
        BuildResolutionDropdown();
        RegisterEvents();
    }

    void OnEnable()
    {
        RefreshUI();
    }

    private void RegisterEvents()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    private void BuildResolutionDropdown()
    {
        resolutions.Clear();

        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        Resolution[] screenResolutions = Screen.resolutions;

        for (int i = 0; i < screenResolutions.Length; i++)
        {
            int width = screenResolutions[i].width;
            int height = screenResolutions[i].height;

            if (ContainsResolution(width, height)) continue;

            resolutions.Add(new Vector2Int(width, height));
            options.Add(width + " x " + height);
        }

        if (resolutions.Count == 0)
        {
            resolutions.Add(new Vector2Int(Screen.width, Screen.height));
            options.Add(Screen.width + " x " + Screen.height);
        }

        resolutionDropdown.AddOptions(options);
    }

    private bool ContainsResolution(int width, int height)
    {
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].x == width && resolutions[i].y == height)
                return true;
        }

        return false;
    }

    private int FindResolutionIndex(int width, int height)
    {
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].x == width && resolutions[i].y == height)
                return i;
        }

        return Mathf.Clamp(resolutions.Count - 1, 0, resolutions.Count - 1);
    }

    private void RefreshUI()
    {
        if (GameSettingsManager.Instance == null) return;

        isRefreshing = true;

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(GameSettingsManager.Instance.MasterVolume);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.SetValueWithoutNotify(GameSettingsManager.Instance.BGMVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(GameSettingsManager.Instance.SFXVolume);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(GameSettingsManager.Instance.IsFullscreen);

        if (resolutionDropdown != null)
        {
            int index = FindResolutionIndex(
                GameSettingsManager.Instance.ScreenWidth,
                GameSettingsManager.Instance.ScreenHeight
            );

            resolutionDropdown.SetValueWithoutNotify(index);
            resolutionDropdown.RefreshShownValue();
        }

        isRefreshing = false;
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (isRefreshing) return;
        if (GameSettingsManager.Instance == null) return;

        GameSettingsManager.Instance.SetMasterVolume(value);
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (isRefreshing) return;
        if (GameSettingsManager.Instance == null) return;

        GameSettingsManager.Instance.SetBGMVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (isRefreshing) return;
        if (GameSettingsManager.Instance == null) return;

        GameSettingsManager.Instance.SetSFXVolume(value);
    }

    private void OnResolutionChanged(int index)
    {
        if (isRefreshing) return;
        if (GameSettingsManager.Instance == null) return;
        if (index < 0 || index >= resolutions.Count) return;

        Vector2Int selected = resolutions[index];
        bool fullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;

        GameSettingsManager.Instance.SetResolution(selected.x, selected.y, fullscreen);
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        if (isRefreshing) return;
        if (GameSettingsManager.Instance == null) return;
        if (resolutions.Count == 0) return;

        int index = resolutionDropdown != null
            ? resolutionDropdown.value
            : FindResolutionIndex(Screen.width, Screen.height);

        index = Mathf.Clamp(index, 0, resolutions.Count - 1);

        Vector2Int selected = resolutions[index];
        GameSettingsManager.Instance.SetResolution(selected.x, selected.y, isFullscreen);
    }
}