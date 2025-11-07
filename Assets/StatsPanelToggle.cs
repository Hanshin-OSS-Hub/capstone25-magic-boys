using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsPanelToggle : MonoBehaviour
{
    // 전역: 메뉴 열림 여부(다른 스크립트에서 읽기만)
    public static bool UIBlocked { get; private set; }

    [Header("Refs")]
    public GameObject statsPanel;                 // 스탯 버튼/표시 패널(켜졌다 꺼질 대상)
    public TestPlayerController controller;       // 플레이어 컨트롤러 (커서/입력 잠금)
    public PlayerStats player;                    // 포인트 표시용(옵션)

    [Header("UI (optional)")]
    public TMP_Text pointText;
    public Button btnSTR, btnDEX, btnMAG, btnLUK;

    [Header("Key")]
    public KeyCode toggleKey = KeyCode.K;

    [Header("Disable when open (optional)")]
    public MonoBehaviour[] disableDuringMenu;     // 예: TestPlayerAttack 등 입력 스크립트들

    bool paused = false;

    void Awake()
    {
        if (!controller) controller = FindObjectOfType<TestPlayerController>();
        if (!player) player = FindObjectOfType<PlayerStats>();

        if (!statsPanel)
            Debug.LogWarning("[StatsPanelToggle] statsPanel not assigned!");

        //항상 켜져있는 패널에 붙여야함.
        if (gameObject == statsPanel)
            Debug.LogWarning("[StatsPanelToggle] Attach to Canvas (not to the panel that gets disabled).");
    }

    void Start()
    {
        Show(false); // 시작은 닫힘
        Debug.Log("[StatsPanelToggle] Ready. Press 'K' to toggle.");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Show(!paused);
    }

    public void Show(bool on)
    {
        paused = on;

        if (statsPanel) statsPanel.SetActive(on);

        Time.timeScale = on ? 0f : 1f;                 // 일시정지/재개
        if (controller) controller.SetUIFocus(on);     // 커서/입력 잠금 전환

        UIBlocked = on;                                // ★ 전역 차단 플래그

        // (옵션) 특정 컴포넌트 껐다 켜기
        if (disableDuringMenu != null)
        {
            foreach (var c in disableDuringMenu)
                if (c) c.enabled = !on;
        }

        RefreshUI();
        Debug.Log($"[StatsPanelToggle] {(on ? "OPEN" : "CLOSE")} | timescale={Time.timeScale}");
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
        if (player != null) player.OnStatPointChanged += OnPointChanged;
    }
    void OnDisable()
    {
        if (player != null) player.OnStatPointChanged -= OnPointChanged;
    }
    void OnPointChanged(int _) => RefreshUI();
}