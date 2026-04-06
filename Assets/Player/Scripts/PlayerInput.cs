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
    public bool IsRightClickPressed { get; private set; }

    // 마법 키 입력 (Inspector에서 변경 가능)
    [Header("Skill Keys")]
    public KeyCode skill1Key = KeyCode.Alpha1;
    public KeyCode skill2Key = KeyCode.Alpha2;
    public KeyCode skill3Key = KeyCode.Alpha3;
    public KeyCode skill4Key = KeyCode.Alpha4;
    public KeyCode skill5Key = KeyCode.Alpha5;

    public bool IsSkill1Pressed { get; private set; }
    public bool IsSkill2Pressed { get; private set; }
    public bool IsSkill3Pressed { get; private set; }
    public bool IsSkill4Pressed { get; private set; }
    public bool IsSkill5Pressed { get; private set; }

    void Update()
    {
        // GetKeyDown은 누르는 순간만 true
        IsJumpPressed = Input.GetKeyDown(KeyCode.Space);
        IsInteractPressed = Input.GetKeyDown(KeyCode.F);
        IsDropPressed = Input.GetKeyDown(KeyCode.G);

        IsAttackPressed = Input.GetMouseButtonDown(0);
        IsRightClickPressed = Input.GetMouseButtonDown(1);

        IsSkill1Pressed = Input.GetKeyDown(skill1Key);
        IsSkill2Pressed = Input.GetKeyDown(skill2Key);
        IsSkill3Pressed = Input.GetKeyDown(skill3Key);
        IsSkill4Pressed = Input.GetKeyDown(skill4Key);
        IsSkill5Pressed = Input.GetKeyDown(skill5Key);

        // 누르고 있는 동안 true
        IsRunPressed = Input.GetKey(KeyCode.LeftShift);
        IsCrouchPressed = Input.GetKey(KeyCode.LeftControl);

        // 이동 입력
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // 마우스 입력
        MouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }
}