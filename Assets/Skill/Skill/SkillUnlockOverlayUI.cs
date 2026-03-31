using UnityEngine;

public class SkillUnlockOverlayUI : MonoBehaviour
{
    [Header("잠금 오버레이 5개 (Q, E, R, T, Y 순서)")]
    public GameObject[] lockOverlays = new GameObject[5];

    void Awake()
    {
        RefreshUI();
    }

    void Start()
    {
        RefreshUI();

        if (SkillProgressionManager.Instance != null)
            SkillProgressionManager.Instance.OnChanged += RefreshUI;
    }

    void OnEnable()
    {
        RefreshUI();
    }

    void OnDestroy()
    {
        if (SkillProgressionManager.Instance != null)
            SkillProgressionManager.Instance.OnChanged -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (SkillProgressionManager.Instance == null) return;

        for (int i = 0; i < lockOverlays.Length; i++)
        {
            if (lockOverlays[i] == null) continue;

            bool unlocked = SkillProgressionManager.Instance.IsUnlocked(i);
            lockOverlays[i].SetActive(!unlocked);
        }
    }
}