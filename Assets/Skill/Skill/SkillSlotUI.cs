using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    public int slotIndex;           // 0~4
    public Button button;
    public GameObject lockOverlay;

    void Start()
    {
        Refresh();

        if (SkillProgressionManager.Instance != null)
            SkillProgressionManager.Instance.OnChanged += Refresh;
    }

    void OnDestroy()
    {
        if (SkillProgressionManager.Instance != null)
            SkillProgressionManager.Instance.OnChanged -= Refresh;
    }

    void Refresh()
    {
        if (SkillProgressionManager.Instance == null) return;

        bool unlocked = SkillProgressionManager.Instance.IsUnlocked(slotIndex);

        if (lockOverlay) lockOverlay.SetActive(!unlocked);
        if (button) button.interactable = unlocked;
    }
}
