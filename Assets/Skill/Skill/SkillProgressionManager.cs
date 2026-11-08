using System;
using UnityEngine;

public class SkillProgressionManager : MonoBehaviour
{
    public static SkillProgressionManager Instance { get; private set; }

    public const int MaxSkills = 5;

    // Demo: Q/E/R/T unlocked from the start, Y (meteor) comes from the puzzle-room chest
    public const int StartingSkills = 4;

    // TEST: unlock every skill (including Y meteor) for skill testing. Set to false for the real demo flow.
    public static bool TestUnlockAll = true;

    // 1이면 스킬1만 활성화, 2면 스킬1~2 활성화
    public int UnlockedSkillCount { get; private set; } = StartingSkills;

    public event Action OnChanged;

    private const string KEY = "UnlockedSkillCount";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    private void Load()
    {
        UnlockedSkillCount = PlayerPrefs.GetInt(KEY, StartingSkills);
        UnlockedSkillCount = Mathf.Clamp(UnlockedSkillCount, StartingSkills, MaxSkills);
    }

    public bool IsUnlocked(int slotIndex0Based)
    {
        if (TestUnlockAll) return slotIndex0Based >= 0 && slotIndex0Based < MaxSkills;
        return slotIndex0Based >= 0 && slotIndex0Based < UnlockedSkillCount;
    }

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

        if (Instance != null)
        {
            Instance.UnlockedSkillCount = StartingSkills;
            Instance.OnChanged?.Invoke();
        }
    }
}