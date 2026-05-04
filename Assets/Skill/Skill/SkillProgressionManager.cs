using System;
using UnityEngine;

public class SkillProgressionManager : MonoBehaviour
{
    public static SkillProgressionManager Instance { get; private set; }

    public const int MaxSkills = 5;

    // 1이면 스킬1만 활성화, 2면 스킬1~2 활성화
    public int UnlockedSkillCount { get; private set; } = 1;

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
        UnlockedSkillCount = PlayerPrefs.GetInt(KEY, 1);
        UnlockedSkillCount = Mathf.Clamp(UnlockedSkillCount, 1, MaxSkills);
    }

    public bool IsUnlocked(int slotIndex0Based)
    {
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
            Instance.UnlockedSkillCount = 1;
            Instance.OnChanged?.Invoke();
        }
    }
}