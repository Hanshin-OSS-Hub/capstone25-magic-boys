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


    // 매 프레임 입력을 감지하고 저장
    void Update()
    {
        // GetKeyDown은 키를 누르는 순간에만 true
        IsJumpPressed = Input.GetKeyDown(KeyCode.Space);
        IsInteractPressed = Input.GetKeyDown(KeyCode.F);
        IsDropPressed = Input.GetKeyDown(KeyCode.G);

        // GetKey는 키를 누르고 있는 동안 계속 true
        IsRunPressed = Input.GetKey(KeyCode.LeftShift);
        IsCrouchPressed = Input.GetKey(KeyCode.LeftControl);

        // WASD 입력
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        
        // 마우스 움직임 입력
        MouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        // "Fire1" == '마우스 좌클릭'
        IsAttackPressed = Input.GetButtonDown("Fire1");
    }
}