using UnityEngine;
using UnityEngine.EventSystems; // ★ UI 위 입력 차단용

[RequireComponent(typeof(CharacterController))]
public class TestPlayerController : MonoBehaviour
{
    [Header("References")]
    public PlayerStats stats;            // 연결되면 stats.moveSpeed 사용
    public Transform cameraPivot;        // 보통 Main Camera (플레이어 자식)

    [Header("Movement")]
    public float moveSpeed = 5f;         // stats 없을 때 기본 속도
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 200f;
    public float pitchClamp = 45f;       // 상하 회전 제한
    public bool invertY = false;

    [Header("State")]
    public bool inputLocked = false;     // UI 열릴 때 true (StatsPanelToggle에서 제어)
    float yaw, pitch;
    float verticalVelocity;

    CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // 초기 시야 값 맞추기
        yaw = transform.eulerAngles.y;
        if (cameraPivot) pitch = cameraPivot.localEulerAngles.x;

        // 기본은 전투 모드(커서 잠금)
        SetUIFocus(false);
    }

    void Update()
    {
        // ★ UI 위에서 포인터가 올라가 있으면(버튼/슬라이더 등) 조작 입력 무시
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // UI가 열려 있으면 캐릭터 조작 정지
        if (!inputLocked)
        {
            HandleLook();
            HandleMove();
            HandleJump();
        }
    }

    // === 외부(UI 토글 등)에서 호출: 커서/입력 상태 전환 ===
    public void SetUIFocus(bool uiFocus)
    {
        inputLocked = uiFocus;
        Cursor.lockState = uiFocus ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiFocus;
    }

    void HandleLook()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mx;
        pitch += (invertY ? my : -my);
        pitch = Mathf.Clamp(pitch, -pitchClamp, pitchClamp);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleMove()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 카메라 기준 이동
        Vector3 f = cameraPivot ? cameraPivot.forward : transform.forward;
        Vector3 r = cameraPivot ? cameraPivot.right : transform.right;
        f.y = 0f; r.y = 0f; f.Normalize(); r.Normalize();

        // PlayerStats가 있으면 그 이동속도 사용(DEX 반영)
        float speed = stats ? stats.moveSpeed : moveSpeed;

        Vector3 input = (f * v + r * h).normalized * speed;

        // 중력
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // 지면 붙게 살짝 눌러줌
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = input;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleJump()
    {
        if (!controller.isGrounded) return;

        if (Input.GetButtonDown("Jump"))
        {
            // v = sqrt(2gh)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}