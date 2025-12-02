using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("필수 컴포넌트")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Camera cam;

    [Header("이동 설정")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float crouchMoveSpeed = 2f;
    public float mouseSensitivity = 180f;
    public float jumpForce = 5f;
    public LayerMask groundMask;

    [Header("앉기 설정 (Scale 값)")]
    public float standingScaleY = 1f;
    public float crouchingScaleY = 0.5f;
    public float crouchSpeed = 8f;

    [Header("플레이어 상태")]
    public bool isRun = false;
    public bool isCrouch = false;
    public bool isMove = false;
    public bool isGround = false;

    private float currentSpeed;
    private float targetScaleY;
    private float pitch = 0f;        // 카메라 상하
    private float yawDeltaFixed = 0; // FixedUpdate에서 쓸 수평 회전 델타 누적

    private bool currentFootState;
    private bool previousFootState;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();
        playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        currentSpeed = walkSpeed;
        targetScaleY = standingScaleY;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        int playerLayer = LayerMask.NameToLayer("Player");
        int heldLayer   = LayerMask.NameToLayer("HeldItem");
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (playerLayer >= 0 && heldLayer >= 0) Physics.IgnoreLayerCollision(playerLayer, heldLayer, true);
        if (heldLayer >= 0 && groundLayer >= 0) Physics.IgnoreLayerCollision(heldLayer, groundLayer, true);

        // 넘어짐 방지: X/Z 회전만 고정 (Y 고정 금지)
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        // 마우스 입력(프레임 독립)
        Vector2 mouse = playerInput.MouseInput;
        float dt = Time.unscaledDeltaTime;

        // 상하(피치): 카메라 로컬 회전
        pitch -= mouse.y * mouseSensitivity * dt;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // 좌우: 물리 회전과 일치시키기 위해 FixedUpdate에서 처리 -> 델타만 누적
        yawDeltaFixed += mouse.x * mouseSensitivity * dt;

        HandleCrouch(playerInput.IsCrouchPressed);
        Run(playerInput.IsRunPressed); // crouch 처리 이후
        Jump(playerInput.IsJumpPressed);
        GroundCheck(); // 디버그 레이 포함
    }

    void FixedUpdate()
    {
        // 수평 회전은 물리 틱에서 rb.MoveRotation으로 적용
        if (Mathf.Abs(yawDeltaFixed) > 0.0001f)
        {
            Quaternion add = Quaternion.Euler(0f, yawDeltaFixed, 0f);
            rb.MoveRotation(rb.rotation * add);
            yawDeltaFixed = 0f;
        }

        Move(playerInput.MoveInput);
    }

    private void Move(Vector2 moveInput)
    {
        isMove = moveInput.sqrMagnitude > 0.0001f;

        Vector3 moveDir = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        Vector3 desired = moveDir * currentSpeed;

        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(desired.x, v.y, desired.z);
    }

    private void Run(bool isRunPressed)
    {
        if (isCrouch) { isRun = false; currentSpeed = crouchMoveSpeed; return; }
        isRun = isRunPressed;
        currentSpeed = isRun ? runSpeed : walkSpeed;
    }

    private void HandleCrouch(bool isCrouchPressed)
    {
        // 달리는 중에는 앉기 금지
        if (isRun && isCrouchPressed) return;

        isCrouch = isCrouchPressed;
        targetScaleY = isCrouch ? crouchingScaleY : standingScaleY;

        // 속도 스위치
        currentSpeed = isCrouch ? crouchMoveSpeed : (isRun ? runSpeed : walkSpeed);

        // 스케일 보간
        Vector3 s = transform.localScale;
        float newY = Mathf.Lerp(s.y, targetScaleY, Time.deltaTime * crouchSpeed);
        transform.localScale = new Vector3(s.x, newY, s.z);
    }

    private void Jump(bool isJumpPressed)
    {
        if (isJumpPressed && isGround)
        {
            // 수직 속도 리셋 후 즉시 임펄스
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
    }

    private void GroundCheck()
    {
        // 스케일 기반 레이 길이 
        float rayDistance = transform.localScale.y * 1.0f + 0.25f;
        Vector3 origin = transform.position + Vector3.up * 0.1f; // 살짝 위에서 쏘면 노이즈 감소
        isGround = Physics.Raycast(origin, Vector3.down, rayDistance, groundMask, QueryTriggerInteraction.Ignore);

        currentFootState = isGround;

        previousFootState = currentFootState;

        Debug.DrawRay(origin, Vector3.down * rayDistance, isGround ? Color.green : Color.red);
    }
}
