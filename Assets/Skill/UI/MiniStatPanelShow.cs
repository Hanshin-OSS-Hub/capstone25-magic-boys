using UnityEngine;

public class MiniStatPanelShow : MonoBehaviour
{
    [Header("Target")]
    public GameObject targetPanel;

    [Header("Key")]
    public KeyCode holdKey = KeyCode.C;

    void Awake()
    {
        if (!targetPanel)
            targetPanel = gameObject;
    }

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        bool showByHold = Input.GetKey(holdKey);
        bool showByStatPanel = StatsPanelToggle.UIBlocked;

        bool shouldShow = showByHold || showByStatPanel;

        if (targetPanel && targetPanel.activeSelf != shouldShow)
            targetPanel.SetActive(shouldShow);
    }
}