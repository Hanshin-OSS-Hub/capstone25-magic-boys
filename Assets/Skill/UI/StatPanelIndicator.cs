using UnityEngine;
using UnityEngine.UI;

public class StatPanelIndicator : MonoBehaviour
{
    [Header("Target")]
    public Image target;               // 이 Image의 sprite를 바꿈(비우면 자동 GetComponent)
    public Sprite closedSprite;        // 이미지1 (패널 닫힘/포인트 0)
    public Sprite openSprite;          // 이미지2 (패널 열림)

    [Header("Data")]
    public PlayerStats player;         // 남은 포인트 확인용(비우면 자동 검색)

    [Header("Optional")]
    public Button targetButton;        // 옵션: 버튼도 같이 on/off 하고 싶을 때
    public bool disableButtonWhenNoPoints = false;

    bool lastAppliedOpenState;

    void Awake()
    {
        if (!target) target = GetComponent<Image>();
        if (!player) player = FindObjectOfType<PlayerStats>();
        if (!targetButton) targetButton = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (player != null) player.OnStatPointChanged += OnPointsChanged;
        Refresh(force: true);
    }

    void OnDisable()
    {
        if (player != null) player.OnStatPointChanged -= OnPointsChanged;
    }

    void Update()
    {
        // 패널 열림/닫힘 상태는 StatsPanelToggle.UIBlocked 로 파악
        Refresh();
    }

    void OnPointsChanged(int _)
    {
        Refresh(force: true);
    }

    void Refresh(bool force = false)
    {
        int points = player ? player.statPoints : 0;
        bool panelOpen = StatsPanelToggle.UIBlocked;

        // 포인트가 0이면 무조건 closedSprite
        bool useOpenSprite = panelOpen && points > 0;

        if (!force && useOpenSprite == lastAppliedOpenState) return;

        lastAppliedOpenState = useOpenSprite;
        if (target)
            target.sprite = useOpenSprite ? openSprite : closedSprite;

        if (targetButton && disableButtonWhenNoPoints)
            targetButton.interactable = points > 0;
    }
}