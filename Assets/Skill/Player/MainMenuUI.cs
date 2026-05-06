using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuUI : MonoBehaviour
{
    [Header("Scene")]
    public string firstGameSceneName = "Stage1";

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    [Header("Main Buttons")]
    public Button newGameButton;
    public Button continueButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Settings Button")]
    public Button settingsBackButton;

    void Awake()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(NewGame);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(CloseSettings);
    }

    void Start()
    {
        Time.timeScale = 1f;

        PauseMenuUI.ForcePausedState(false);
        StatsPanelToggle.ForceUIBlocked(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        RefreshContinueButton();
    }

    private void RefreshContinueButton()
    {
        if (continueButton == null) return;

        continueButton.interactable = GameSceneSavePoint.HasSavedScene();
    }

    public void NewGame()
    {
        Time.timeScale = 1f;

        PlayerStats.ResetSavedData();
        SkillProgressionManager.ResetSavedData();
        GameSceneSavePoint.ResetSavedScene();

        PlayerPrefs.SetString(GameSceneSavePoint.LastSceneKey, firstGameSceneName);
        PlayerPrefs.Save();

        DungeonGenerator.saveSeed = -1;
        SceneManager.LoadScene(firstGameSceneName);
    }

    public void ContinueGame()
    {
        Time.timeScale = 1f;

        string sceneName = PlayerPrefs.GetString(GameSceneSavePoint.LastSceneKey, firstGameSceneName);
        SceneManager.LoadScene(sceneName);
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}