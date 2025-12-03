using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    // 이동/시점
    public Vector2 MoveInput { get; private set; }
    public Vector2 MouseInput { get; private set; }

    // 버튼류
    public bool IsRunPressed { get; private set; }
    public bool IsCrouchPressed { get; private set; }
    public bool IsJumpPressed { get; private set; }
    public bool IsInteractPressed { get; private set; } // F (근접 등)
    public bool IsDropPressed { get; private set; } // G
    public bool IsAttackPressed { get; private set; } // LMB (Fire1)

    // 추가
    public bool IsSkill1Pressed { get; private set; } // Q
    public bool IsToggleStatsPressed { get; private set; } // K

    void Update()
    {
        // edge
        IsJumpPressed = Input.GetKeyDown(KeyCode.Space);
        IsInteractPressed = Input.GetKeyDown(KeyCode.F);
        IsDropPressed = Input.GetKeyDown(KeyCode.G);
        IsAttackPressed = Input.GetButtonDown("Fire1");

        // 추가 edge
        IsSkill1Pressed = Input.GetKeyDown(KeyCode.Q);
        IsToggleStatsPressed = Input.GetKeyDown(KeyCode.K);

        // hold
        IsRunPressed = Input.GetKey(KeyCode.LeftShift);
        IsCrouchPressed = Input.GetKey(KeyCode.LeftControl);

        // axes
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        MouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }
}