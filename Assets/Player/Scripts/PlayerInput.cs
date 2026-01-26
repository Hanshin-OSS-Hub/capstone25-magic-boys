using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    // 외부에서(예: StatsPanelToggle) 막을 때 사용
    public bool Blocked { get; private set; } = false;
    public void SetBlocked(bool on) => Blocked = on;

    // 축 입력
    public Vector2 MoveInput { get; private set; }
    public Vector2 MouseInput { get; private set; }

    // 에지(Down) 입력
    public bool IsJumpPressed { get; private set; }  // Space
    public bool IsInteractPressed { get; private set; }  // F
    public bool IsDropPressed { get; private set; }  // G
    public bool IsAttackPressed { get; private set; }  // LMB (Fire1)
    public bool IsSkill1Pressed { get; private set; }  // Q
    public bool IsSkill2Pressed { get; private set; }  // E
    public bool IsToggleStatsPressed { get; private set; }  // K
    public bool IsRollPressed { get; private set; }  // LeftCtrl (구르기)

    // 홀드 상태가 필요한 것들
    public bool IsRunHeld => Input.GetKey(KeyCode.LeftShift);
    public bool IsRollHeld => Input.GetKey(KeyCode.LeftControl);
    public bool IsAttackHeld => Input.GetButton("Fire1");

    void Update()
    {
        // 패널 열림(전역) 또는 외부 차단 플래그면 입력 모두 0/false
        if (Blocked || StatsPanelToggle.UIBlocked)
        {
            ClearAll();
            return;
        }

        // --- 에지(Down) ---
        IsJumpPressed = Input.GetKeyDown(KeyCode.Space);
        IsInteractPressed = Input.GetKeyDown(KeyCode.F);
        IsDropPressed = Input.GetKeyDown(KeyCode.G);
        IsAttackPressed = Input.GetButtonDown("Fire1");
        IsSkill1Pressed = Input.GetKeyDown(KeyCode.Q);
        IsSkill2Pressed = Input.GetKeyDown(KeyCode.E);
        IsToggleStatsPressed = Input.GetKeyDown(KeyCode.K);
        IsRollPressed = Input.GetKeyDown(KeyCode.LeftControl);

        // --- 축 ---
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        MouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }

    void ClearAll()
    {
        MoveInput = MouseInput = Vector2.zero;

        IsJumpPressed = IsInteractPressed = IsDropPressed =
        IsAttackPressed = IsSkill1Pressed = IsSkill2Pressed =
        IsToggleStatsPressed = IsRollPressed = false;
    }
}