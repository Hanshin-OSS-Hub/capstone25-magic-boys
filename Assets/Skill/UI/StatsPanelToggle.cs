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
        // 시작할 때 K 패널 상태 초기화
        kPanelOpen = false;
        UIBlocked = false;

        // 시작할 때 스텟창 전체 강제로 끄기
        currentPanelVisible = false;

        if (statsPanel != null)
            statsPanel.SetActive(false);

        // 시작할 때 시간 정상화
        Time.timeScale = 1f;

        // 시작할 때 커서 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 혹시 비활성화했던 스크립트 있으면 다시 켜기
        SetDisableScripts(true);

        RefreshUI();
    }

    void Update()
    {
        // ESC 메뉴가 열려 있으면 K/C 스텟창 조작 안 함
        if (PauseMenuUI.IsPaused)
            return;

        // K키: 스텟 찍는 창 열기/닫기
        if (Input.GetKeyDown(toggleKey))
        {
            SetKPanel(!kPanelOpen);
            return;
        }

        // K 패널이 닫혀 있을 때만 C키로 잠깐 보기 가능
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

        SetDisableScripts(!on);

        RefreshUI();
    }

    private void ApplyPanelVisible(bool visible)
    {
        // 여기서 같은 상태라고 return 하면 시작할 때 패널이 안 꺼질 수 있음
        currentPanelVisible = visible;

        if (statsPanel != null)
            statsPanel.SetActive(visible);

        RefreshUI();
    }

    private void SetDisableScripts(bool enabled)
    {
        if (disableDuringMenu == null) return;

        foreach (MonoBehaviour component in disableDuringMenu)
        {
            if (component != null)
                component.enabled = enabled;
        }
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

        // K로 열었을 때만 스텟 찍기 버튼 활성화
        // C로 볼 때는 보기만 가능
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