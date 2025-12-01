using UnityEngine;
using StarterAssets;   // StarterAssetsInputs 사용하려고 필요

public class PlayerRoll : MonoBehaviour
{
    [Header("입력 설정")]
    public float doubleTapTime = 0.25f; // 스페이스 두 번 누르는 최대 시간
    private float lastSpaceTime = -999f;

    [Header("구르기 설정")]
    public float rollDuration = 0.8f;   // 구르기 지속시간 (애니메이션 길이랑 비슷하게)
    
    private Animator animator;
    private int rollHash;

    public bool IsRolling { get; private set; }
    private float rollTimer;

    private Vector3 rollDirection;

    // 이동/카메라 기준을 맞추기 위한 참조
    private StarterAssetsInputs _input;
    private Transform _cameraTransform;

    void Awake()
    {
        // 플레이어에 붙어있는 StarterAssetsInputs 가져오기
        _input = GetComponent<StarterAssetsInputs>();
        if (_input == null)
        {
            Debug.LogError("PlayerRoll: StarterAssetsInputs를 찾지 못했습니다.");
        }

        // 메인 카메라 Transform
        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rollHash = Animator.StringToHash("Roll");
    }

    void Update()
    {
        // 구르는 중이면 타이머만 감소
        if (IsRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f)
                IsRolling = false;
            return;
        }

        // 1) Ctrl 눌렀을 때 즉시 구르기
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            TryRoll();
            return;
        }

        // 2) 스페이스 더블탭 구르기
        if (Input.GetKeyDown(KeyCode.Space))
        {
            float now = Time.time;
            if (now - lastSpaceTime <= doubleTapTime)
            {
                TryRoll();
                lastSpaceTime = -999f; // 중복 인식 방지
                return;
            }

            lastSpaceTime = now;
        }
    }

    void TryRoll()
    {
        if (_input == null) return;

        //  이동에서 쓰는 것과 똑같이, StarterAssetsInputs의 move 값 사용
        Vector2 move = _input.move;  // (-1~1, -1~1)
        Vector3 inputDir = new Vector3(move.x, 0f, move.y).normalized;

        // 방향키 안 눌렀으면 정면으로
        if (inputDir == Vector3.zero)
            inputDir = Vector3.forward;

        //  카메라 기준으로 방향 회전 (이동 코드랑 같은 방식)
        float yaw = 0f;
        if (_cameraTransform != null)
        {
            yaw = _cameraTransform.eulerAngles.y;
        }

        rollDirection = Quaternion.Euler(0f, yaw, 0f) * inputDir;

        // 구르기 애니메이션 실행
        if (animator != null)
        {
            animator.SetTrigger(rollHash);
        }

        // 구르기 상태 시작
        IsRolling = true;
        rollTimer = rollDuration;
    }

    public Vector3 GetRollDirection()
    {
        return rollDirection;
    }
}
