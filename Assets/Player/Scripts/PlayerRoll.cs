using UnityEngine;

public class PlayerRoll : MonoBehaviour
{
    [Header("입력 설정")]
    public float doubleTapTime = 0.25f; // 스페이스 두 번 누르는 최대 시간
    float lastTapTime = 0f;

    [Header("구르기 설정")]
    public float rollDuration = 0.8f;   // 구르기 지속시간
    public float rollSpeed = 8f;        // 구르는 속도

    private Animator animator;
    private int rollHash;

    public bool IsRolling { get; private set; }
    private float rollTimer;

    private Vector3 rollDirection;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rollHash = Animator.StringToHash("Roll");
    }

    void Update()
    {
        // 구르는 중이면 상태 유지
        if (IsRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f)
                IsRolling = false;
            return;
        }

        // -----------------------------------
        // 1) Ctrl 눌렀을 때 즉시 구르기
    
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            TryRoll();
            return;
        }

        // -----------------------------------
        // 2) 스페이스 더블탭 구르기
        // -----------------------------------
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.time - lastTapTime <= doubleTapTime)
            {
                TryRoll();
                lastTapTime = 0; // 중복 인식 방지
                return;
            }
            lastTapTime = Time.time;
        }
    }

    void TryRoll()
    {
        // WASD 방향 입력 확인
        Vector3 move = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            0f,
            Input.GetAxisRaw("Vertical")
        ).normalized;

        // 방향키 안 눌렀으면 정면으로 구르기
        if (move == Vector3.zero)
            move = transform.forward;

        rollDirection = move;

        // 구르기 애니메이션 실행
        animator.SetTrigger(rollHash);

        // 롤 시작
        IsRolling = true;
        rollTimer = rollDuration;
    }

    public Vector3 GetRollDirection()
    {
        return rollDirection;
    }
}
