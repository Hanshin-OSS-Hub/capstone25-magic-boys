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

    // 0 = 구르기 시작, 1 = 구르기 끝. 이동 속도 곡선에 사용
    public float RollProgress => rollDuration > 0f ? Mathf.Clamp01(1f - rollTimer / rollDuration) : 1f;

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
        // 대각선 포함 8방향: 현재 이동 입력을 우선 사용하고, 없으면 직전 방향키 버퍼 사용
        Vector3 inputDir = Vector3.zero;
        if (_input != null && _input.move.sqrMagnitude > 0.01f)
            inputDir = new Vector3(_input.move.x, 0f, _input.move.y).normalized;
        else
            inputDir = GetBufferedDirection();

        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;

        if (inputDir == Vector3.zero)
        {
            // 입력이 없으면 캐릭터가 바라보는 방향으로 구르기
            rollDirection = transform.forward;
        }
        else
        {
            float yaw = _cameraTransform != null ? _cameraTransform.eulerAngles.y : transform.eulerAngles.y;
            rollDirection = Quaternion.Euler(0f, yaw, 0f) * inputDir;
        }

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