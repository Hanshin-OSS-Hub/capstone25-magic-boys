using System.Collections;
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

        // 메인메뉴에 들어왔을 때 기존 Player가 남아있으면
        // 삭제하지 말고 숨김만 처리
        PersistentPlayer.HideCurrent();

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

    private bool isLoading;

    public void NewGame() => BeginGame(true);

    public void ContinueGame() => BeginGame(false);

    private void BeginGame(bool newGame)
    {
        if (isLoading) return;
        string sceneName = newGame ? firstGameSceneName
            : PlayerPrefs.GetString(GameSceneSavePoint.LastSceneKey, firstGameSceneName);
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[MainMenu] Cannot load scene: " + sceneName);
            return;
        }

        isLoading = true;
        if (newGameButton != null) newGameButton.interactable = false;
        if (continueButton != null) continueButton.interactable = false;
        StartCoroutine(LoadGame(newGame, sceneName));
    }

    private IEnumerator LoadGame(bool newGame, string sceneName)
    {
        Time.timeScale = 1f;
        PauseMenuUI.ForcePausedState(false);
        StatsPanelToggle.ForceUIBlocked(false);

        Debug.Log($"[MainMenu] {(newGame ? "NewGame" : "Continue")} requested; releasing previous Player.");
        PersistentPlayer.HideCurrent();
        PersistentPlayer.DestroyCurrent();

        // Destroy is deferred. Finish renderer/Player cleanup before starting
        // scene loading, which otherwise competes for Unity's asset locks.
        yield return null;

        if (newGame)
        {
            PlayerStats.ResetSavedData();
            SkillProgressionManager.ResetSavedData();
            GameSceneSavePoint.ResetSavedScene();
            PlayerPrefs.SetString(GameSceneSavePoint.LastSceneKey, firstGameSceneName);
            PlayerPrefs.Save();
            DungeonGenerator.saveSeed = -1;
        }

        Debug.Log("[MainMenu] Player cleanup complete; loading " + sceneName);
        AsyncOperation load = null;
        try { load = SceneManager.LoadSceneAsync(sceneName); }
        catch (System.Exception error) { Debug.LogException(error); }
        if (load != null) yield return load;
        else
        {
            isLoading = false;
            if (newGameButton != null) newGameButton.interactable = true;
            RefreshContinueButton();
        }
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