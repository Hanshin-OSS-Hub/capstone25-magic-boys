using UnityEngine;
using StarterAssets;

public enum moveState
{
    None,
    W,
    A,
    S,
    D
}

public class PlayerRoll : MonoBehaviour
{
    [Header("입력 설정")]
    public float doubleTapTime = 0.25f;
    private float lastSpaceTime = -999f;

    [Header("방향 선입력 버퍼")]
    public float directionBufferTime = 0.15f;

    [Header("구르기 설정")]
    public float rollDuration = 0.8f;

    public moveState mvstate = moveState.None;

    private Animator animator;
    private int rollHash;

    public bool IsRolling { get; private set; }
    private float rollTimer;

    private Vector3 rollDirection;

    private StarterAssetsInputs _input;
    private Transform _cameraTransform;

    private float lastDirectionInputTime = -999f;

    void Awake()
    {
        _input = GetComponent<StarterAssetsInputs>();
        if (_input == null)
        {
            Debug.LogError("PlayerRoll: StarterAssetsInputs를 찾지 못했습니다.");
        }

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
        UpdateMoveState();

        if (IsRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f)
                IsRolling = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            TryRoll();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            float now = Time.time;
            if (now - lastSpaceTime <= doubleTapTime)
            {
                TryRoll();
                lastSpaceTime = -999f;
                return;
            }

            lastSpaceTime = now;
        }
    }

    void UpdateMoveState()
    {
        // 가장 최근에 눌린 방향을 우선 저장
        if (Input.GetKeyDown(KeyCode.W))
        {
            mvstate = moveState.W;
            lastDirectionInputTime = Time.time;
            return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            mvstate = moveState.A;
            lastDirectionInputTime = Time.time;
            return;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            mvstate = moveState.S;
            lastDirectionInputTime = Time.time;
            return;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            mvstate = moveState.D;
            lastDirectionInputTime = Time.time;
            return;
        }

        // 키를 계속 누르고 있는 경우도 유지
        if (Input.GetKey(KeyCode.W))
        {
            mvstate = moveState.W;
            lastDirectionInputTime = Time.time;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            mvstate = moveState.A;
            lastDirectionInputTime = Time.time;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            mvstate = moveState.S;
            lastDirectionInputTime = Time.time;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            mvstate = moveState.D;
            lastDirectionInputTime = Time.time;
        }
    }

    void TryRoll()
    {
        Vector3 inputDir = GetBufferedDirection();

        // 아무 방향 입력이 없으면 카메라 기준 정면
        if (inputDir == Vector3.zero)
            inputDir = Vector3.forward;

        float yaw = 0f;
        if (_cameraTransform != null)
        {
            yaw = _cameraTransform.eulerAngles.y;
        }

        rollDirection = Quaternion.Euler(0f, yaw, 0f) * inputDir;

        // 구르기 시작 전에 방향 먼저 맞추기
        Vector3 flatDir = new Vector3(rollDirection.x, 0f, rollDirection.z);
        if (flatDir != Vector3.zero)
        {
            transform.forward = flatDir;
        }

        if (animator != null)
        {
            animator.SetTrigger(rollHash);
        }

        IsRolling = true;
        rollTimer = rollDuration;
    }

    Vector3 GetBufferedDirection()
    {
        // 최근 입력이 버퍼 시간 안에 있으면 그 방향 사용
        if (Time.time - lastDirectionInputTime <= directionBufferTime)
        {
            switch (mvstate)
            {
                case moveState.W:
                    return Vector3.forward;

                case moveState.A:
                    return Vector3.left;

                case moveState.S:
                    return Vector3.back;

                case moveState.D:
                    return Vector3.right;
            }
        }

        return Vector3.zero;
    }

    public Vector3 GetRollDirection()
    {
        return rollDirection;
    }
}