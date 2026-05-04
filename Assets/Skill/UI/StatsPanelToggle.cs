using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsPanelToggle : MonoBehaviour
{
    public static bool UIBlocked { get; private set; }

    [Header("Keys")]
    public KeyCode toggleKey = KeyCode.K;
    public KeyCode holdViewKey = KeyCode.C;

    [Header("Refs")]
    public GameObject statsPanel;
    public PlayerStats player;

    [Header("UI")]
    public TMP_Text pointText;
    public Button btnSTR;
    public Button btnDEX;
    public Button btnMAG;
    public Button btnLUK;

    [Header("Disable when K panel open")]
    public MonoBehaviour[] disableDuringMenu;

    private bool kPanelOpen = false;
    private bool currentPanelVisible = false;

    void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerStats>();

        if (statsPanel == null)
            Debug.LogWarning("[StatsPanelToggle] statsPanel not assigned!");
    }

    void OnEnable()
    {
        if (player != null)
            player.OnStatPointChanged += OnPointChanged;
    }

    void OnDisable()
    {
        if (player != null)
            player.OnStatPointChanged -= OnPointChanged;
    }

    void Start()
    {
        SetKPanel(false);
        ApplyPanelVisible(false);
    }

    void Update()
    {
        if (PauseMenuUI.IsPaused) return;

        if (Input.GetKeyDown(toggleKey))
        {
            SetKPanel(!kPanelOpen);
            return;
        }

        if (!kPanelOpen)
        {
            bool holdVisible = Input.GetKey(holdViewKey);
            ApplyPanelVisible(holdVisible);
        }
    }

    private void SetKPanel(bool on)
    {
        kPanelOpen = on;
        UIBlocked = on;

        ApplyPanelVisible(on);

        Time.timeScale = on ? 0f : 1f;

        Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = on;

        if (disableDuringMenu != null)
        {
            foreach (MonoBehaviour c in disableDuringMenu)
            {
                if (c != null)
                    c.enabled = !on;
            }
        }

        RefreshUI();
    }

    private void ApplyPanelVisible(bool visible)
    {
        if (currentPanelVisible == visible) return;

        currentPanelVisible = visible;

        if (statsPanel != null)
            statsPanel.SetActive(visible);

        RefreshUI();
    }

    public void Open()
    {
        SetKPanel(true);
    }

    public void Close()
    {
        SetKPanel(false);
    }

    private void RefreshUI()
    {
        if (player == null) return;

        if (pointText != null)
            pointText.text = "Points: " + player.statPoints;

        bool canSpend = kPanelOpen && player.statPoints > 0;

        if (btnSTR != null) btnSTR.interactable = canSpend;
        if (btnDEX != null) btnDEX.interactable = canSpend;
        if (btnMAG != null) btnMAG.interactable = canSpend;
        if (btnLUK != null) btnLUK.interactable = canSpend;
    }

    private void OnPointChanged(int point)
    {
        RefreshUI();
    }

    public void ClickSTR()
    {
        if (!kPanelOpen) return;

        if (player != null && player.AllocateStat(StatType.STR))
            player.Save();
    }

    public void ClickDEX()
    {
        if (!kPanelOpen) return;

        if (player != null && player.AllocateStat(StatType.DEX))
            player.Save();
    }

    public void ClickMAG()
    {
        if (!kPanelOpen) return;

        if (player != null && player.AllocateStat(StatType.MAG))
            player.Save();
    }

    public void ClickLUK()
    {
        if (!kPanelOpen) return;

        if (player != null && player.AllocateStat(StatType.LUK))
            player.Save();
    }

    public static void ForceUIBlocked(bool blocked)
    {
        UIBlocked = blocked;
    }
}