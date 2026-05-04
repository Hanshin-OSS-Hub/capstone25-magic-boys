using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseMenuUI : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Pause Buttons")]
    public Button resumeButton;
    public Button mainMenuButton;
    public Button quitButton;
    public Button settingsButton;

    [Header("Settings Button")]
    public Button settingsBackButton;

    void Awake()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(CloseSettings);
    }

    void Start()
    {
        IsPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // K 스텟창이 열려 있으면 ESC 메뉴와 충돌하지 않게 막음
        if (!IsPaused && StatsPanelToggle.UIBlocked) return;

        if (!IsPaused)
        {
            PauseGame();
        }
        else
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
                CloseSettings();
            else
                ResumeGame();
        }
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void GoToMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static void ForcePausedState(bool paused)
    {
        IsPaused = paused;
    }
}