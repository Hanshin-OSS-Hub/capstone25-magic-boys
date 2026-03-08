using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private PlayerAttack _attack;
        private PlayerRoll _roll;

        [Header("Roll Settings")]
        public float RollSpeed = 8.0f;       // 구르는 이동 속도
        public float RollDuration = 0.7f;    // 구르기 지속 시간
        public float RollCooldown = 0.3f;    // 구르기 후 잠깐 딜레이
        [HideInInspector] public bool IsLockOn = false;
        [HideInInspector] public Transform LockOnTarget = null;


        private bool _isRolling;
        private float _rollTimer;
        private float _rollCooldownTimer;
        private Vector3 _rollDirection;

        private int _animIDRoll;

        private float _lastJumpKeyTime = -999f;
        public float DoubleTapTime = 0.25f;  // 스페이스 두 번 입력 간격

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null;
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif
            _attack = GetComponent<PlayerAttack>(); 
            _roll = GetComponent<PlayerRoll>();


            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            HandleRollInput();

            JumpAndGravity();
            GroundedCheck();
            Move();
        }
                private void HandleRollInput()
        {
            // 쿨타임 처리
            if (_rollCooldownTimer > 0f)
            {
                _rollCooldownTimer -= Time.deltaTime;
            }

            // 구르는 중이면 타이머만 감소시키고 종료 시점 체크
            if (_isRolling)
            {
                _rollTimer -= Time.deltaTime;
                if (_rollTimer <= 0f)
                {
                    _isRolling = false;
                    _rollCooldownTimer = RollCooldown;
                }
                return;
            }

#if ENABLE_INPUT_SYSTEM
            // 스페이스 두 번 직접 감지
#endif
            if (Input.GetKeyDown(KeyCode.Space))
            {
                float now = Time.time;

                // 두 번 연속, 그리고 움직이는 중이고, 땅에 있을 때만 구르기
                if (now - _lastJumpKeyTime <= DoubleTapTime &&
                    _input.move != Vector2.zero &&
                    Grounded &&
                    _rollCooldownTimer <= 0f)
                {
                    StartRoll();
                    // 점프 입력은 취소해서 기본 점프가 발동하지 않도록
                    _input.jump = false;
                }

                _lastJumpKeyTime = now;
            }
        }

        private void StartRoll()
        {
            // 현재 이동 입력 방향 기준으로 구르기 방향 계산
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (inputDirection == Vector3.zero)
            {
                // 입력이 없다면 앞으로
                inputDirection = Vector3.forward;
            }

            // 카메라 기준 방향으로 회전
            float targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                   _mainCamera.transform.eulerAngles.y;

            _rollDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

            _isRolling = true;
            _rollTimer = RollDuration;

            if (_hasAnimator)
            {
                _animator.SetTrigger(_animIDRoll);
            }
        }


        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDRoll = Animator.StringToHash("Roll");
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(
                transform.position.x,
                transform.position.y - GroundedOffset,
                transform.position.z);

            Grounded = Physics.CheckSphere(
                spherePosition,
                GroundedRadius,
                GroundLayers,
                QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
{
    // 락온 중이면 보스를 바라보도록 각도 계산
    if (IsLockOn && LockOnTarget != null)
    {
        Vector3 dir = LockOnTarget.position - CinemachineCameraTarget.transform.position;

        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            Vector3 euler = lookRot.eulerAngles;

            // Yaw(좌우), Pitch(상하) 추출
            _cinemachineTargetYaw = euler.y;

            float pitch = euler.x;
            // 0~360 -> -180~180 보정 (위/아래 각도 자연스럽게)
            if (pitch > 180f) pitch -= 360f;
            _cinemachineTargetPitch = pitch;
        }
    }
    else
    {
        // 평소에는 기존 마우스 입력으로 회전
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw   += _input.look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
        }
    }

    // 공통 클램프
    _cinemachineTargetYaw   = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
    _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

    // 실제 카메라 타깃 회전 적용
    CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
        _cinemachineTargetPitch + CameraAngleOverride,
        _cinemachineTargetYaw,
        0.0f);
}

        private void Move()
{
    // 구르는 중이면 롤 전용 이동만 처리
    if (_roll != null && _roll.IsRolling)
    {
        Vector3 rollDir = _roll.GetRollDirection();   // 미리 저장된 구르기 방향
        float rollSpeed = 8f;                         // 롤 속도 (원하는 값으로 조정)

        // 수평 이동 + 중력
        Vector3 horizontal = rollDir * rollSpeed;
        Vector3 vertical = new Vector3(0.0f, _verticalVelocity, 0.0f);

        _controller.Move((horizontal + vertical) * Time.deltaTime);

        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, 0.0f);
            _animator.SetFloat(_animIDMotionSpeed, 0.0f);
        }

        return;
    }

    // 공격 중에는 제자리에서 중력만 적용
    if (_attack != null && _attack.IsAttacking)
    {
        _controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, 0.0f);
            _animator.SetFloat(_animIDMotionSpeed, 0.0f);
        }

        return;
    }

    //평상시 이동 처리
    float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

    if (_input.move == Vector2.zero)
        targetSpeed = 0.0f;

    float currentHorizontalSpeed =
        new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

    float speedOffset = 0.1f;
    float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

    if (currentHorizontalSpeed < targetSpeed - speedOffset ||
        currentHorizontalSpeed > targetSpeed + speedOffset)
    {
        _speed = Mathf.Lerp(
            currentHorizontalSpeed,
            targetSpeed * inputMagnitude,
            Time.deltaTime * SpeedChangeRate);

        _speed = Mathf.Round(_speed * 1000f) / 1000f;
    }
    else
    {
        _speed = targetSpeed;
    }

    _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
    if (_animationBlend < 0.01f)
        _animationBlend = 0f;

    Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

    if (_input.move != Vector2.zero)
    {
        _targetRotation =
            Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
            _mainCamera.transform.eulerAngles.y;

        float rotation = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            _targetRotation,
            ref _rotationVelocity,
            RotationSmoothTime);

        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
    }

    Vector3 targetDirection =
        Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

    _controller.Move(
        targetDirection.normalized * (_speed * Time.deltaTime) +
        new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

    if (_hasAnimator)
    {
        _animator.SetFloat(_animIDSpeed, _animationBlend);
        _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
    }
}



        private void JumpAndGravity()
        {
            if (Grounded)
            {
                if (_roll != null && _roll.IsRolling) return;
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
                {
                    if (FootstepAudioClips != null && FootstepAudioClips.Length > 0)
                            {
                    var index = Random.Range(0, FootstepAudioClips.Length);
            var clip = FootstepAudioClips[index];
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(
                clip,
                transform.TransformPoint(_controller.center),
                FootstepAudioVolume);
        }
    }
}

        private void OnLand(AnimationEvent animationEvent)
{
    if (animationEvent.animatorClipInfo.weight > 0.5f)
    {
        if (LandingAudioClip == null) return;

        AudioSource.PlayClipAtPoint(
            LandingAudioClip,
            transform.TransformPoint(_controller.center),
            FootstepAudioVolume);
    }
}
    }
}