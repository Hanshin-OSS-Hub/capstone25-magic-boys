using UnityEngine;

public class StatsPanelToggle : MonoBehaviour
{
    public static bool UIBlocked { get; private set; }

    [Header("Refs")]
    public GameObject statsPanel;

    private bool isOpen = false;

    void Start()
    {
        if (statsPanel != null)
            statsPanel.SetActive(false);

        UIBlocked = false;
        SetCursorState(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            TogglePanel();
        }
    }

    void TogglePanel()
    {
        isOpen = !isOpen;

        if (statsPanel != null)
            statsPanel.SetActive(isOpen);

        UIBlocked = isOpen;
        SetCursorState(isOpen);
    }

    void SetCursorState(bool show)
    {
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }
}