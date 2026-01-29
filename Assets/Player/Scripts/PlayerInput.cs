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
    public bool IsJumpPressed { get; private set; }          // Space
    public bool IsInteractPressed { get; private set; }      // F
    public bool IsDropPressed { get; private set; }          // G
    public bool IsAttackPressed { get; private set; }        // LMB (Fire1)
    public bool IsSkill1Pressed { get; private set; }        // Q
    public bool IsSkill2Pressed { get; private set; }        // E
    public bool IsSkill3Pressed { get; private set; }        // R
    public bool IsSkill4Pressed { get; private set; }        // T
    public bool IsSkill5Pressed { get; private set; }        // Y
    public bool IsToggleStatsPressed { get; private set; }   // K
    public bool IsRollPressed { get; private set; }          // LeftCtrl (구르기)

    // 홀드 상태가 필요한 것들
    public bool IsRunHeld => Input.GetKey(KeyCode.LeftShift);
    public bool IsRollHeld => Input.GetKey(KeyCode.LeftControl);
    public bool IsAttackHeld => Input.GetButton("Fire1");

    void Update()
    {
        // ✅ 토글(K)은 UI가 열려 있어도 항상 읽는다 (닫기 가능하게)
        IsToggleStatsPressed = Input.GetKeyDown(KeyCode.K);

        // 패널 열림(전역) 또는 외부 차단 플래그면 "나머지 입력"만 0/false
        if (Blocked || StatsPanelToggle.UIBlocked)
        {
            ClearAllExceptToggle();
            return;
        }

        // --- 에지(Down) ---
        IsJumpPressed = Input.GetKeyDown(KeyCode.Space);
        IsInteractPressed = Input.GetKeyDown(KeyCode.F);
        IsDropPressed = Input.GetKeyDown(KeyCode.G);

        IsAttackPressed = Input.GetButtonDown("Fire1");

        IsSkill1Pressed = Input.GetKeyDown(KeyCode.Q);
        IsSkill2Pressed = Input.GetKeyDown(KeyCode.E);
        IsSkill3Pressed = Input.GetKeyDown(KeyCode.R);
        IsSkill4Pressed = Input.GetKeyDown(KeyCode.T);
        IsSkill5Pressed = Input.GetKeyDown(KeyCode.Y);

        IsRollPressed = Input.GetKeyDown(KeyCode.LeftControl);

        // --- 축 ---
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        MouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }

    void ClearAllExceptToggle()
    {
        MoveInput = MouseInput = Vector2.zero;

        IsJumpPressed = false;
        IsInteractPressed = false;
        IsDropPressed = false;
        IsAttackPressed = false;

        IsSkill1Pressed = false;
        IsSkill2Pressed = false;
        IsSkill3Pressed = false;
        IsSkill4Pressed = false;
        IsSkill5Pressed = false;

        IsRollPressed = false;

        // IsToggleStatsPressed 는 여기서 건드리지 않음!
    }
}
