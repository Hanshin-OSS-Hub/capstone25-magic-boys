using TMPro;
using UnityEngine;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsPanelToggle : MonoBehaviour
{
    public static bool UIBlocked { get; private set; }

    [Header("Refs")]
    public PlayerInput input;           // ← 새로 추가 (씬의 PlayerInput 참조)
    public GameObject statsPanel;       // 열고 닫을 패널
    public PlayerStats player;

    [Header("UI (optional)")]
    public TMP_Text pointText;
    public Button btnSTR, btnDEX, btnMAG, btnLUK;

    [Header("Disable when open (optional)")]
    public MonoBehaviour[] disableDuringMenu;

    bool paused = false;

    void Awake()
    {
        if (!input) input = FindObjectOfType<PlayerInput>();
        if (!player) player = FindObjectOfType<PlayerStats>();

        if (!statsPanel)
            Debug.LogWarning("[StatsPanelToggle] statsPanel not assigned!");
    }

    void Start()
    {
        Show(false);
    }

    void Update()
    {
        if (input != null && input.IsToggleStatsPressed)
            Show(!paused);
    }

    public void Show(bool on)
    {
        paused = on;

        if (statsPanel) statsPanel.SetActive(on);

        // 게임 정지/재개
        Time.timeScale = on ? 0f : 1f;

        // 커서 전환(컨트롤러 의존 제거)
        Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = on;

        UIBlocked = on;

        if (disableDuringMenu != null)
        {
            foreach (var c in disableDuringMenu)
                if (c) c.enabled = !on;
        }

        RefreshUI();
    }

    public void Open() => Show(true);
    public void Close() => Show(false);

    void RefreshUI()
    {
        if (!player) return;

        if (pointText) pointText.text = $"Points: {player.statPoints}";
        bool canSpend = player.statPoints > 0;

        if (btnSTR) btnSTR.interactable = canSpend;
        if (btnDEX) btnDEX.interactable = canSpend;
        if (btnMAG) btnMAG.interactable = canSpend;
        if (btnLUK) btnLUK.interactable = canSpend;
    }

    void OnEnable()
    {
        if (player) player.OnStatPointChanged += OnPointChanged;
    }
    void OnDisable()
    {
        if (player) player.OnStatPointChanged -= OnPointChanged;
    }
    void OnPointChanged(int _) => RefreshUI();
}