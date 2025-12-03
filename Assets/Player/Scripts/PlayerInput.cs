using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    // 외부(UI 등)에서 on/off
    public bool Blocked { get; private set; } = false;
    public void SetBlocked(bool on) => Blocked = on;

    // 이동/시점
    public Vector2 MoveInput { get; private set; }
    public Vector2 MouseInput { get; private set; }

    // 버튼(에지)
    public bool IsRunPressed { get; private set; }
    public bool IsCrouchPressed { get; private set; }
    public bool IsJumpPressed { get; private set; }   // Space (Down)
    public bool IsInteractPressed { get; private set; }   // F (Down)
    public bool IsDropPressed { get; private set; }   // G (Down)
    public bool IsAttackPressed { get; private set; }   // LMB (Down)
    public bool IsSkill1Pressed { get; private set; }   // Q (Down)
    public bool IsSkill2Pressed { get; private set; }   // E (Down)
    public bool IsToggleStatsPressed { get; private set; } // K (Down)

    // (옵션) 홀드 상태가 필요하면 이렇게도 노출
    public bool IsRunHeld => Input.GetKey(KeyCode.LeftShift);
    public bool IsCrouchHeld => Input.GetKey(KeyCode.LeftControl);
    public bool IsAttackHeld => Input.GetButton("Fire1");

    void Update()
    {
        if (Blocked)
        {
            // 패널 열렸을 때 깔끔히 모두 0으로
            MoveInput = MouseInput = Vector2.zero;
            IsRunPressed = IsCrouchPressed = IsJumpPressed =
            IsInteractPressed = IsDropPressed = IsAttackPressed =
            IsSkill1Pressed = IsSkill2Pressed = IsToggleStatsPressed = false;
            return;
        }

        // 에지(Down)
        IsJumpPressed = Input.GetKeyDown(KeyCode.Space);
        IsInteractPressed = Input.GetKeyDown(KeyCode.F);
        IsDropPressed = Input.GetKeyDown(KeyCode.G);
        IsAttackPressed = Input.GetButtonDown("Fire1");
        IsSkill1Pressed = Input.GetKeyDown(KeyCode.Q);
        IsSkill2Pressed = Input.GetKeyDown(KeyCode.E);
        IsToggleStatsPressed = Input.GetKeyDown(KeyCode.K);

        // 홀드(프레임 내 안내용) — 필요하면 읽고, 아니면 무시
        IsRunPressed = Input.GetKey(KeyCode.LeftShift);
        IsCrouchPressed = Input.GetKey(KeyCode.LeftControl);

        // 축
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        MouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }
}