using UnityEngine;

public class StagePuzzleChestReward : MonoBehaviour
{
    [Range(1, 5)]
    public int stageIndex = 1;

    public void GiveReward()
    {
        Debug.Log("GiveReward 호출됨 / stageIndex = " + stageIndex);

        if (SkillProgressionManager.Instance != null)
            SkillProgressionManager.Instance.RewardFromStage(stageIndex);
        else
            Debug.LogWarning("SkillProgressionManager.Instance가 null이라 보상 지급 실패");
    }
}