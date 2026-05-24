using UnityEngine;

public class StagePuzzleChestReward : MonoBehaviour
{
    [Header("Reward")]
    [Range(1, 5)]
    public int stageIndex = 1;

    [Header("Portal")]
    [Tooltip("큐브를 깨면 활성화할 포탈 오브젝트")]
    [SerializeField] private GameObject portalToShow;

    [Tooltip("시작할 때 포탈을 자동으로 숨길지")]
    [SerializeField] private bool hidePortalOnStart = true;

    private bool rewardGiven = false;

    private void Start()
    {
        if (hidePortalOnStart && portalToShow != null)
        {
            portalToShow.SetActive(false);
        }
    }

    public void GiveReward()
    {
        if (rewardGiven) return;
        rewardGiven = true;

        if (SkillProgressionManager.Instance != null)
        {
            SkillProgressionManager.Instance.RewardFromStage(stageIndex);
            Debug.Log($"[StagePuzzleChestReward] Stage {stageIndex} 보상 지급 완료");
        }
        else
        {
            Debug.LogWarning("[StagePuzzleChestReward] SkillProgressionManager.Instance가 없음");
        }

        ShowPortal();
    }

    private void ShowPortal()
    {
        if (portalToShow == null)
        {
            Debug.LogWarning("[StagePuzzleChestReward] portalToShow가 연결되지 않음");
            return;
        }

        portalToShow.SetActive(true);
        Debug.Log("[StagePuzzleChestReward] 포탈 활성화 완료");
    }
}