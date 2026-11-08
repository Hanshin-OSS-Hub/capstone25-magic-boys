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
        [Header("Player - Speed")]
        [Tooltip("걷기 속도 (Left Alt 누르고 있을 때)")]
        public float WalkSpeed = 2.0f;

        [Tooltip("기본 이동 속도. 실제 액션 게임처럼 기본이 달리기")]
        public float RunSpeed = 5.0f;

        [Tooltip("질주 속도 (Shift)")]
        public float DashSpeed = 7.0f;

        [Tooltip("걷기 전환 키 (누르고 있는 동안 걷기)")]
        public KeyCode WalkKey = KeyCode.LeftAlt;

        [Header("Player - Acceleration")]
        [Tooltip("출발 가속도 (m/s^2). 높을수록 즉각 반응")]
        public float Acceleration = 35.0f;

        [Tooltip("멈출 때 감속도 (m/s^2). 낮을수록 미끄러지듯 멈춤")]
        public float Deceleration = 28.0f;

        [Tooltip("반대 방향으로 급선회할 때 가속도")]
        public float TurnAcceleration = 60.0f;

        [Tooltip("공중 방향 전환 가속도. 낮을수록 점프 관성 유지")]
        public float AirAcceleration = 10.0f;

        [Tooltip("공격 시작 시 미끄러지며 멈추는 감속도")]
        public float AttackDeceleration = 20.0f;

        [Header("Player - Rotation")]
        [Tooltip("정지/저속일 때 회전 속도 (deg/s)")]
        public float TurnSpeedIdle = 1500.0f;

        [Tooltip("최고 속도일 때 회전 속도 (deg/s). 낮을수록 크게 돌며 회전")]
        public float TurnSpeedMoving = 720.0f;

        [Tooltip("공중 회전 속도 배율")]
        [Range(0f, 1f)] public float AirTurnMultiplier = 0.5f;

        [Tooltip("공격 시작 직후 입력/락온 방향으로 몸을 돌리는 시간")]
        public float AttackTurnWindow = 0.15f;

        [Header("Player - Animation")]
        [Tooltip("애니메이션 블렌드 반응 속도")]
        public float AnimationBlendRate = 12.0f;

        [Tooltip("질주 시 달리기 애니메이션 재생 배율 (발 미끄러짐 감소)")]
        public float DashAnimationSpeed = 1.2f;

        [Header("Player - Combat Movement")]
        [Tooltip("콤보 매 타마다 앞으로 살짝 내딛는 속도 (m/s)")]
        public float AttackLungeSpeed = 3.5f;

        [Tooltip("3타(강타) 내딛기 배율")]
        public float FinisherLungeMultiplier = 1.4f;

        [Tooltip("락온 중 가만히 있을 때 대상을 바라보는 회전 속도 (deg/s)")]
        public float LockOnFacingSpeed = 540f;

        [Tooltip("락온 시 카메라가 대상을 따라가는 부드러움 (클수록 빠름)")]
        public float LockOnCameraSharpness = 10f;

        [Header("Audio")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Tooltip("이 시간 이상 공중에 있다가 착지했을 때만 착지 소리 (계단에서 소리 나는 것 방지)")]
        public float MinLandingAirTime = 0.35f;

        [Header("Ground Snap")]
        [Tooltip("계단/내리막에서 발이 뜨지 않도록 바닥에 붙이는 최대 거리")]
        public float GroundSnapDistance = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Tooltip("떨어질 때 중력 배율. 1보다 크면 둥실거리지 않고 묵직한 점프")]
        public float FallGravityMultiplier = 1.6f;

        [Tooltip("발판에서 떨어진 직후에도 점프를 허용하는 시간 (코요테 타임)")]
        public float CoyoteTime = 0.12f;

        [Tooltip("착지 직전에 누른 점프를 기억하는 시간 (선입력)")]
        public float JumpBufferTime = 0.15f;

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

        private Vector3 _horizontalVelocity;
        private float _animationBlend;
        private float _verticalVelocity;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private int _lastComboStep;
        private bool _wasGroundedLast;
        private float _airTime;
        private float _lastAirTime;
        private AudioSource _sfxSource;
        private float _attackTurnTimer;
        private Vector3 _attackFacingDir;
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
        public float RollSpeed = 12f;

        [Tooltip("구르기 진행도(0~1)에 따른 속도 배율. 초반에 튀어나가고 끝에서 감속")]
        public AnimationCurve RollSpeedCurve = new AnimationCurve(
            new Keyframe(0.0f, 1.25f),
            new Keyframe(0.35f, 1.05f),
            new Keyframe(0.75f, 0.6f),
            new Keyframe(1.0f, 0.3f));
        [HideInInspector] public bool IsLockOn = false;
        [HideInInspector] public Transform LockOnTarget = null;
        /// <summary>락온 시 카메라가 바라볼 지점 (LockOnTarget 스크립트가 매 프레임 갱신). 없으면 대상 위치 + 1.2m</summary>
        [HideInInspector] public Vector3? LockOnPoint = null;

        /// <summary>현재 수평 이동 속도 (m/s)</summary>
        public float PlanarSpeed => _horizontalVelocity.magnitude;

        /// <summary>달리기→질주 사이 0~1 (카메라 FOV 연출용)</summary>
        public float DashAmount01 =>
            DashSpeed > RunSpeed ? Mathf.Clamp01((PlanarSpeed - RunSpeed) / (DashSpeed - RunSpeed)) : 0f;

        // 보스의 CC기 패턴에 사용
        private bool _isBeingKnockedBack = false;
        private Vector3 _knockbackVelocity;

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

            _attack = GetComponent<PlayerAttack>();
            _roll = GetComponent<PlayerRoll>();

            AssignAnimationIDs();

            // 발소리/착지 소리 전용 오디오 소스 (피치를 조금씩 바꿔 반복감 줄임)
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0.6f;
            _sfxSource.minDistance = 2f;
            _sfxSource.maxDistance = 25f;

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        public void ResetAfterTeleport()
        {
            // Rigidbody와 별도로 보관하던 낙하·밀림 속도도 전환 때 한 번 초기화한다.
            StopAllCoroutines();
            _verticalVelocity = -2f;
            _knockbackVelocity = Vector3.zero;
            _isBeingKnockedBack = false;
            _horizontalVelocity = Vector3.zero;
            _animationBlend = 0f;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _airTime = 0f;
            _lastAirTime = 0f;
            _wasGroundedLast = false;
            Grounded = true;
            _fallTimeoutDelta = FallTimeout;
            _jumpTimeoutDelta = JumpTimeout;
            if (_input != null)
            {
                _input.move = Vector2.zero;
                _input.jump = false;
            }
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
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

            // 체공 시간 기록 (짧은 체공 = 계단, 착지 소리 생략)
            if (Grounded)
            {
                if (_airTime > 0f) _lastAirTime = _airTime;
                _airTime = 0f;
            }
            else
            {
                _airTime += Time.deltaTime;
            }

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            if (IsLockOn && LockOnTarget != null)
            {
                Vector3 aim = LockOnPoint ?? (LockOnTarget.position + Vector3.up * 1.2f);
                Vector3 dir = aim - CinemachineCameraTarget.transform.position;

                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dir);
                    Vector3 euler = lookRot.eulerAngles;

                    float pitch = euler.x;
                    if (pitch > 180f) pitch -= 360f;
                    // 대상을 화면 약간 위쪽에 두도록 살짝 내려다봄
                    pitch = Mathf.Clamp(pitch + 8f, -20f, 35f);

                    // 즉시 고정하지 않고 부드럽게 따라감
                    float k = 1f - Mathf.Exp(-LockOnCameraSharpness * Time.deltaTime);
                    _cinemachineTargetYaw = Mathf.LerpAngle(_cinemachineTargetYaw, euler.y, k);
                    _cinemachineTargetPitch = Mathf.Lerp(_cinemachineTargetPitch, pitch, k);
                }
            }
            else
            {
                if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
                {
                    float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                    _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                    _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
                }
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw,
                0.0f);
        }

        private void Move()
        {
            float dt = Time.deltaTime;

            if (_isBeingKnockedBack)
            {
                _knockbackVelocity.y += Gravity * dt;
                _controller.Move(_knockbackVelocity * dt);
                _horizontalVelocity = Vector3.zero;
                return;
            }

            Vector2 move = _input.move;
            bool hasInput = move.sqrMagnitude > 0.01f;
            float inputMagnitude = _input.analogMovement ? Mathf.Clamp01(move.magnitude) : 1f;
            Vector3 desiredDir = hasInput ? CameraRelativeDirection(move) : Vector3.zero;

            // 구르기: 초반에 튀어나가고 끝에서 감속하는 속도 곡선
            if (_roll != null && _roll.IsRolling)
            {
                Vector3 rollDir = _roll.GetRollDirection();
                rollDir.y = 0f;
                if (rollDir.sqrMagnitude > 0.0001f) rollDir.Normalize();

                float curve = RollSpeedCurve != null ? RollSpeedCurve.Evaluate(_roll.RollProgress) : 1f;
                // 구르기 끝 속도가 그대로 남아 달리기로 자연스럽게 이어짐
                _horizontalVelocity = rollDir * (RollSpeed * curve);

                _controller.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);
                SnapToGround();
                return;
            }

            // 공격: 콤보 매 타마다 입력/락온 방향으로 몸을 돌리고 살짝 앞으로 내딛음
            bool attacking = _attack != null && _attack.IsAttacking;
            int comboStep = attacking ? _attack.ComboStep : 0;
            if (comboStep > 0 && comboStep != _lastComboStep)
            {
                BeginAttackFacing(desiredDir, hasInput);
                Vector3 lungeDir = _attackFacingDir.sqrMagnitude > 0f ? _attackFacingDir : transform.forward;
                bool finisher = _attack.comboStates != null && comboStep == _attack.comboStates.Length;
                if (Grounded)
                    _horizontalVelocity = lungeDir * AttackLungeSpeed * (finisher ? FinisherLungeMultiplier : 1f);
            }
            _lastComboStep = comboStep;

            float targetSpeed = 0f;
            if (hasInput && !attacking)
            {
                float baseSpeed = _input.sprint ? DashSpeed : (IsWalkHeld() ? WalkSpeed : RunSpeed);
                targetSpeed = baseSpeed * inputMagnitude;
            }

            Vector3 targetVelocity = desiredDir * targetSpeed;

            float accel;
            if (!Grounded)
                accel = AirAcceleration;
            else if (attacking)
                accel = AttackDeceleration;
            else if (targetSpeed < 0.01f)
                accel = Deceleration;
            else if (_horizontalVelocity.sqrMagnitude > 1f &&
                     Vector3.Dot(_horizontalVelocity.normalized, desiredDir) < -0.2f)
                accel = TurnAcceleration; // 급선회(피벗)
            else
                accel = Acceleration;

            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, accel * dt);

            // 회전: 빠를수록 크게 돌고, 정지 상태에선 거의 즉시 방향 전환
            if (attacking)
            {
                if (_attackTurnTimer > 0f && _attackFacingDir.sqrMagnitude > 0.0001f)
                {
                    _attackTurnTimer -= dt;
                    RotateTowards(_attackFacingDir, TurnSpeedIdle * 1.5f, dt);
                }
            }
            else if (hasInput)
            {
                float speed01 = DashSpeed > 0f ? Mathf.Clamp01(_horizontalVelocity.magnitude / DashSpeed) : 0f;
                float turnSpeed = Mathf.Lerp(TurnSpeedIdle, TurnSpeedMoving, speed01);
                if (!Grounded) turnSpeed *= AirTurnMultiplier;
                RotateTowards(desiredDir, turnSpeed, dt);
            }
            else if (IsLockOn && LockOnTarget != null)
            {
                // 락온 중 가만히 있으면 대상을 바라봄
                Vector3 toTarget = LockOnTarget.position - transform.position;
                RotateTowards(toTarget, LockOnFacingSpeed, dt);
            }

            _controller.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);
            SnapToGround();

            UpdateLocomotionAnimation(dt);
        }

        // 계단/내리막에서 한 프레임 떠서 낙하 판정·착지 소리가 나는 것을 방지
        private void SnapToGround()
        {
            bool grounded = _controller.isGrounded;

            if (!grounded && _wasGroundedLast && _verticalVelocity <= 0f && GroundSnapDistance > 0f)
            {
                float radius = _controller.radius * 0.9f;
                Vector3 origin = transform.position + Vector3.up * (radius + 0.05f);

                if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit,
                        GroundSnapDistance + 0.05f, GroundLayers, QueryTriggerInteraction.Ignore)
                    && hit.normal.y > 0.6f)
                {
                    float drop = hit.distance - 0.05f;
                    if (drop > 0.001f)
                    {
                        _controller.Move(Vector3.down * drop);
                    }
                    grounded = true;
                }
            }

            _wasGroundedLast = grounded;
        }

        private void UpdateLocomotionAnimation(float dt)
        {
            if (!_hasAnimator) return;

            float planarSpeed = _horizontalVelocity.magnitude;

            // 블렌드 트리 임계값: 0 = Idle, 2 = Walk, 6 = Run
            float targetBlend;
            if (planarSpeed <= WalkSpeed)
                targetBlend = WalkSpeed > 0f ? planarSpeed / WalkSpeed * 2f : 0f;
            else
                targetBlend = Mathf.Lerp(2f, 6f, Mathf.InverseLerp(WalkSpeed, RunSpeed, planarSpeed));

            _animationBlend = Mathf.Lerp(_animationBlend, targetBlend, 1f - Mathf.Exp(-AnimationBlendRate * dt));
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // 질주 땐 달리기 모션을 빠르게 재생해서 발이 미끄러져 보이지 않게
            float motionSpeed = 1f;
            if (planarSpeed > RunSpeed && DashSpeed > RunSpeed)
                motionSpeed = Mathf.Lerp(1f, DashAnimationSpeed, Mathf.InverseLerp(RunSpeed, DashSpeed, planarSpeed));

            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, motionSpeed);
        }

        private Vector3 CameraRelativeDirection(Vector2 move)
        {
            float yaw = _mainCamera != null ? _mainCamera.transform.eulerAngles.y : transform.eulerAngles.y;
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(move.x, 0f, move.y);
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        }

        private void RotateTowards(Vector3 dir, float degreesPerSecond, float dt)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * dt);
        }

        private void BeginAttackFacing(Vector3 desiredDir, bool hasInput)
        {
            _attackFacingDir = Vector3.zero;

            if (IsLockOn && LockOnTarget != null)
            {
                Vector3 toTarget = LockOnTarget.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f) _attackFacingDir = toTarget.normalized;
            }
            else if (hasInput)
            {
                _attackFacingDir = desiredDir;
            }

            _attackTurnTimer = _attackFacingDir.sqrMagnitude > 0f ? AttackTurnWindow : 0f;
        }

        private bool IsWalkHeld()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(WalkKey);
#elif ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.leftAltKey.isPressed;
#else
            return false;
#endif
        }

        private void JumpAndGravity()
        {
            float dt = Time.deltaTime;
            bool rolling = _roll != null && _roll.IsRolling;

            // 점프 선입력: 착지 직전에 눌러도 착지하는 순간 점프
            if (_input.jump)
            {
                _jumpBufferTimer = JumpBufferTime;
                _input.jump = false;
            }
            else if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer -= dt;
            }

            if (Grounded)
            {
                _coyoteTimer = CoyoteTime;

                if (rolling) return;

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

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= dt;
                }
            }
            else
            {
                if (_coyoteTimer > 0f) _coyoteTimer -= dt;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= dt;
                }
                else if (_hasAnimator)
                {
                    _animator.SetBool(_animIDFreeFall, true);
                }
            }

            // 코요테 타임: 발판 끝에서 살짝 늦게 눌러도 점프됨
            bool canJump = !rolling && _coyoteTimer > 0f && _jumpTimeoutDelta <= 0.0f && _verticalVelocity <= 0.0f;
            if (_jumpBufferTimer > 0f && canJump)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                _jumpTimeoutDelta = JumpTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // 낙하 시 중력 강화 → 둥실거리지 않는 묵직한 점프
            float gravity = _verticalVelocity < 0f ? Gravity * FallGravityMultiplier : Gravity;
            if (_verticalVelocity > -_terminalVelocity)
            {
                _verticalVelocity += gravity * dt;
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

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private int _lastFootstepIndex = -1;

        // 걷기/달리기 애니메이션 이벤트에서 호출
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight < 0.5f) return;
            if (!Grounded || PlanarSpeed < 0.3f) return;
            if (FootstepAudioClips == null || FootstepAudioClips.Length == 0) return;

            // 같은 소리가 연속으로 나지 않게
            int index = Random.Range(0, FootstepAudioClips.Length);
            if (FootstepAudioClips.Length > 1 && index == _lastFootstepIndex)
                index = (index + 1) % FootstepAudioClips.Length;
            _lastFootstepIndex = index;

            var clip = FootstepAudioClips[index];
            if (clip == null) return;

            // 빠를수록 발소리를 조금 크게
            float volume = FootstepAudioVolume * Mathf.Lerp(0.7f, 1.1f, Mathf.InverseLerp(WalkSpeed, DashSpeed, PlanarSpeed));
            PlaySfx(clip, volume, Random.Range(0.92f, 1.08f));
        }

        // 착지 애니메이션 이벤트에서 호출
        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight < 0.5f) return;
            if (LandingAudioClip == null) return;
            // 계단처럼 아주 짧게 뜬 경우엔 착지 소리 없음
            if (_lastAirTime < MinLandingAirTime) return;

            PlaySfx(LandingAudioClip, FootstepAudioVolume, Random.Range(0.95f, 1.05f));
        }

        private void PlaySfx(AudioClip clip, float volume, float pitch)
        {
            if (_sfxSource == null)
            {
                AudioSource.PlayClipAtPoint(clip, transform.TransformPoint(_controller.center), volume);
                return;
            }

            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip, volume);
        }

        public void TakeKnockback(Vector3 impulseForce, float duration = 0.5f)
        {
            StartCoroutine(KnockbackRoutine(impulseForce, duration));
        }

        private System.Collections.IEnumerator KnockbackRoutine(Vector3 impulseForce, float duration)
        {
            _isBeingKnockedBack = true;
            _knockbackVelocity = impulseForce;

            if (_input != null) _input.move = Vector2.zero;

            yield return new WaitForSeconds(duration);

            _isBeingKnockedBack = false;
            _verticalVelocity = _knockbackVelocity.y;
        }
    }
}
