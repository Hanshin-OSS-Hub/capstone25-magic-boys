using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    // 이동 및 시점 조작 입력
    public Vector2 MoveInput { get; private set; }
    public Vector2 MouseInput { get; private set; }

    // 각종 키 입력 상태
    public bool IsRunPressed { get; private set; }
    public bool IsCrouchPressed { get; private set; }
    public bool IsJumpPressed { get; private set; }
    public bool IsInteractPressed { get; private set; }
    public bool IsDropPressed { get; private set; }
    public bool IsAttackPressed { get; private set; }

    // 마법 키 입력
    public bool IsSkillQPressed { get; private set; }
    public bool IsSkillEPressed { get; private set; }
    public bool IsSkillRPressed { get; private set; }
    public bool IsSkillTPressed { get; private set; }
    public bool IsSkillYPressed { get; private set; }

    void Update()
    {
        // GetKeyDown은 누르는 순간만 true
        IsJumpPressed = Input.GetKeyDown(KeyCode.Space);
        IsInteractPressed = Input.GetKeyDown(KeyCode.F);
        IsDropPressed = Input.GetKeyDown(KeyCode.G);

        IsAttackPressed = Input.GetButtonDown("Fire1");

        IsSkillQPressed = Input.GetKeyDown(KeyCode.Q);
        IsSkillEPressed = Input.GetKeyDown(KeyCode.E);
        IsSkillRPressed = Input.GetKeyDown(KeyCode.R);
        IsSkillTPressed = Input.GetKeyDown(KeyCode.T);
        IsSkillYPressed = Input.GetKeyDown(KeyCode.Y);

        // 누르고 있는 동안 true
        IsRunPressed = Input.GetKey(KeyCode.LeftShift);
        IsCrouchPressed = Input.GetKey(KeyCode.LeftControl);

        // 이동 입력
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // 마우스 입력
        MouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }
}