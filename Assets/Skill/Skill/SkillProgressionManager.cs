using System;
using UnityEngine;

public class SkillProgressionManager : MonoBehaviour
{
    public static SkillProgressionManager Instance { get; private set; }

    public const int MaxSkills = 5;

    // 시작 시 항상 첫 번째 스킬만 해금
    public int UnlockedSkillCount { get; private set; } = 1;

    public event Action OnChanged;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        UnlockedSkillCount = 1;
        Debug.Log("SkillProgressionManager Awake / UnlockedSkillCount = " + UnlockedSkillCount);
    }

    public bool IsUnlocked(int slotIndex0Based)
    {
        return slotIndex0Based >= 0 && slotIndex0Based < UnlockedSkillCount;
    }

    // stageIndex: 1~5
    public void RewardFromStage(int stageIndex)
    {
        int target = Mathf.Clamp(stageIndex + 1, 1, MaxSkills);
        Debug.Log($"RewardFromStage 호출 / stageIndex={stageIndex}, target={target}, current={UnlockedSkillCount}");

        if (target <= UnlockedSkillCount)
        {
            Debug.Log("이미 해금되어 있어서 변화 없음");
            return;
        }

        UnlockedSkillCount = target;
        Debug.Log("해금 적용 완료 / 새 UnlockedSkillCount = " + UnlockedSkillCount);

        OnChanged?.Invoke();
    }

    public void ResetProgress()
    {
        UnlockedSkillCount = 1;
        Debug.Log("진행도 초기화 / UnlockedSkillCount = " + UnlockedSkillCount);
        OnChanged?.Invoke();
    }
}