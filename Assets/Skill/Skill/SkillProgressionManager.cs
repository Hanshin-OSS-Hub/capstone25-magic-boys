using System;
using UnityEngine;

public class SkillProgressionManager : MonoBehaviour
{
    public static SkillProgressionManager Instance { get; private set; }

    public const int MaxSkills = 5;

    // 1이면 스킬1만 활성, 2면 스킬1~2 활성...
    public int UnlockedSkillCount { get; private set; } = 1;

    public event Action OnChanged;

    const string KEY = "UnlockedSkillCount";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        UnlockedSkillCount = PlayerPrefs.GetInt(KEY, 1);
        UnlockedSkillCount = Mathf.Clamp(UnlockedSkillCount, 1, MaxSkills);
    }

    public bool IsUnlocked(int slotIndex0Based)
        => slotIndex0Based >= 0 && slotIndex0Based < UnlockedSkillCount;

    // stageIndex: 1~5
    public void RewardFromStage(int stageIndex)
    {
        int target = Mathf.Clamp(stageIndex + 1, 1, MaxSkills);
        if (target <= UnlockedSkillCount) return;

        UnlockedSkillCount = target;
        PlayerPrefs.SetInt(KEY, UnlockedSkillCount);
        PlayerPrefs.Save();

        OnChanged?.Invoke();
    }

    public static void ResetSavedData()
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }
}
