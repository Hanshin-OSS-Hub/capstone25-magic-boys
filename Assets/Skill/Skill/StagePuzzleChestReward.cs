using UnityEngine;

public class StagePuzzleChestReward : MonoBehaviour
{
    [Range(1, 5)]
    public int stageIndex = 1;

    public void GiveReward()
    {
        if (SkillProgressionManager.Instance != null)
            SkillProgressionManager.Instance.RewardFromStage(stageIndex);
    }
}
