using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// 3단 콤보 근접 공격.
// 클릭 → 1타, 1타 중 다시 클릭 → 2타, 2타 중 다시 클릭 → 3타(강타).
// 애니메이터 상태 Attack1/Attack2/Attack3 는 Tools/Magic Boys/Setup Attack Combo 메뉴로 생성한다.
public class PlayerAttack : MonoBehaviour
{
    private Animator animator;

    [Tooltip("콤보가 비정상적으로 끝나지 않을 때 강제로 종료하는 안전 시간(초)")]
    public float comboSafetyTimeout = 3.0f;

    public bool IsAttacking { get; private set; }

    /// <summary>현재 콤보 단계 (0 = 공격 안 함, 1~3)</summary>
    public int ComboStep { get; private set; }

    /// <summary>현재 타격의 데미지 배율 (HammerHitbox에서 사용)</summary>
    public float CurrentDamageMultiplier =>
        ComboStep > 0 && ComboStep <= damageMultiplier.Length ? damageMultiplier[ComboStep - 1] : 1f;

    [Header("Combo")]
    [Tooltip("콤보 단계별 애니메이터 상태 이름")]
    public string[] comboStates = { "Attack1", "Attack2", "Attack3" };

    [Tooltip("타격 판정 시작 (상태 정규화 시간 0~1)")]
    public float[] hitStart = { 0.38f, 0.22f, 0.17f };

    [Tooltip("타격 판정 끝 (상태 정규화 시간 0~1)")]
    public float[] hitEnd = { 0.88f, 0.86f, 0.38f };

    [Tooltip("선입력이 있으면 이 시점에 다음 타로 넘어감")]
    public float[] linkTime = { 0.84f, 0.84f, 1.1f };

    [Tooltip("이 시점 이후에는 이동 입력으로 공격 후딜을 취소할 수 있음")]
    public float[] moveCancelTime = { 0.9f, 0.9f, 0.5f };

    [Tooltip("단계별 데미지 배율")]
    public float[] damageMultiplier = { 1.0f, 1.2f, 2.0f };

    [Tooltip("이 시점 이후의 클릭은 다음 타로 선입력")]
    [Range(0f, 1f)] public float inputBufferOpen = 0.2f;

    public float firstCrossFade = 0.1f;
    public float comboCrossFade = 0.08f;
    public float returnCrossFade = 0.2f;

    [Header("Hit Feel")]
    [Tooltip("타격 순간 잠깐 멈추는 시간 (실시간 초). 0이면 끔")]
    public float hitStopDuration = 0.055f;
    [Tooltip("3타 강타 히트스톱 배율")]
    public float finisherHitStopMultiplier = 1.8f;
    public float hitShakeAmplitude = 0.06f;

    private PlayerInput playerInput;
    private PlayerRoll playerRoll;
    private MagicAttack magicAttack;
    private CameraSwitch cameraSwitch;

    private bool blockAttackUntilRelease = false;
    private bool comboBuffered;
    private float stepStartTime;
    private bool hitWindowOpen;
    private bool hitStopRunning;
    private int[] stateHashes;
    private int locomotionHash;

    [Header("Hammer Hitbox")]
    [SerializeField] private Collider hammerCollider;
    private HammerHitbox hammerHitbox;

    [Header("Chest Hit")]
    public Camera playerCamera;
    public float chestHitRange = 5f;
    public int chestDamage = 1;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();

        playerInput = GetComponent<PlayerInput>();
        playerRoll = GetComponent<PlayerRoll>();
        magicAttack = GetComponent<MagicAttack>();
        cameraSwitch = GetComponentInChildren<CameraSwitch>();

        stateHashes = new int[comboStates.Length];
        for (int i = 0; i < comboStates.Length; i++)
            stateHashes[i] = Animator.StringToHash(comboStates[i]);
        locomotionHash = Animator.StringToHash("Idle Walk Run Blend");

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (hammerCollider != null)
        {
            hammerCollider.enabled = false;
            hammerHitbox = hammerCollider.GetComponent<HammerHitbox>();
        }
        else
        {
            Debug.LogWarning("[PlayerAttack] hammerCollider가 연결되지 않았습니다.");
        }

        if (animator != null && !HasComboStates())
        {
            Debug.LogWarning("[PlayerAttack] 애니메이터에 Attack1~3 상태가 없습니다. Tools/Magic Boys/Setup Attack Combo 를 실행하세요.");
        }
    }

    void Update()
    {
        if (playerInput == null) return;

        if (PauseMenuUI.IsPaused || StatsPanelToggle.UIBlocked)
        {
            DisableHammerCollider();
            return;
        }

        if (magicAttack != null && magicAttack.IsAiming)
        {
            if (IsAttacking) EndCombo(false);
            return;
        }

        if (playerRoll != null && playerRoll.IsRolling)
        {
            // 구르기로 공격 캔슬 (애니메이터의 Attack→Roll 전이가 모션을 처리)
            if (IsAttacking) EndCombo(false);
            blockAttackUntilRelease = true;
            return;
        }

        if (blockAttackUntilRelease)
        {
            if (playerInput.IsAttackPressed)
                return;

            blockAttackUntilRelease = false;
        }

        bool pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool pressed = playerInput.IsAttackPressed && !pointerOverUI;

        if (!IsAttacking)
        {
            if (pressed) StartStep(0);
            return;
        }

        UpdateCombo(pressed);
    }

    private void UpdateCombo(bool pressed)
    {
        int index = ComboStep - 1;
        bool inState = TryGetStepTime(index, out float t);
        float elapsed = Time.time - stepStartTime;

        bool comboReady = HasComboStates();

        // 크로스페이드 직후 몇 프레임은 아직 상태가 바뀌지 않았을 수 있음
        // (예전 단일 Attack 상태는 진입 전이가 0.5초라 더 기다림)
        if (!inState)
        {
            if (elapsed > (comboReady ? 0.15f : 0.7f)) EndCombo(false);
            return;
        }

        if (comboReady && pressed && index < comboStates.Length - 1 && t >= inputBufferOpen)
            comboBuffered = true;

        // 타격 판정: 단계별 유효 구간에서만 망치 콜라이더 ON
        // (콤보 상태가 없으면 예전처럼 공격 상태 전체 구간)
        bool shouldHit = comboReady
            ? t >= Get(hitStart, index, 0.3f) && t <= Get(hitEnd, index, 0.8f)
            : t >= 0.05f && t <= 0.95f;
        if (shouldHit && !hitWindowOpen)
        {
            hitWindowOpen = true;
            if (hammerHitbox != null) hammerHitbox.ResetHitHistory();
            EnableHammerCollider();
        }
        else if (!shouldHit && hitWindowOpen)
        {
            hitWindowOpen = false;
            DisableHammerCollider();
        }

        if (comboBuffered && t >= Get(linkTime, index, 0.85f))
        {
            StartStep(index + 1);
            return;
        }

        if (comboReady && t >= Get(moveCancelTime, index, 0.9f) && playerInput.MoveInput.sqrMagnitude > 0.01f)
        {
            EndCombo(true);
            return;
        }

        if (t >= 0.98f || elapsed > comboSafetyTimeout)
        {
            EndCombo(false);
        }
    }

    private void StartStep(int index)
    {
        if (animator == null || index >= comboStates.Length) return;

        ComboStep = index + 1;
        IsAttacking = true;
        comboBuffered = false;
        stepStartTime = Time.time;
        hitWindowOpen = false;
        DisableHammerCollider();

        if (HasState(stateHashes[index]))
        {
            animator.CrossFadeInFixedTime(stateHashes[index], index == 0 ? firstCrossFade : comboCrossFade, 0, 0f);
        }
        else
        {
            // 콤보 상태가 아직 없으면 예전 단일 공격으로 동작
            animator.SetTrigger("Attack");
        }

        TryHitChest();
    }

    private void EndCombo(bool blendToLocomotion)
    {
        IsAttacking = false;
        ComboStep = 0;
        comboBuffered = false;
        hitWindowOpen = false;
        DisableHammerCollider();

        if (blendToLocomotion && animator != null)
            animator.CrossFadeInFixedTime(locomotionHash, returnCrossFade, 0);
    }

    private bool TryGetStepTime(int index, out float normalizedTime)
    {
        normalizedTime = 0f;
        if (animator == null || index < 0 || index >= stateHashes.Length) return false;

        int hash = stateHashes[index];
        if (animator.IsInTransition(0))
        {
            var next = animator.GetNextAnimatorStateInfo(0);
            if (next.shortNameHash == hash)
            {
                normalizedTime = next.normalizedTime;
                return true;
            }
        }

        var cur = animator.GetCurrentAnimatorStateInfo(0);
        if (cur.shortNameHash == hash)
        {
            // 이 상태에서 빠져나가는 중이면 끝난 것으로 본다
            if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).shortNameHash != hash && cur.normalizedTime > 0.5f)
                return false;

            normalizedTime = cur.normalizedTime;
            return true;
        }

        // 예전 단일 Attack 상태로 동작하는 경우
        if (!HasState(hash))
        {
            if (animator.IsInTransition(0))
            {
                var next = animator.GetNextAnimatorStateInfo(0);
                if (next.IsName("Attack"))
                {
                    normalizedTime = next.normalizedTime;
                    return true;
                }
                if (cur.IsName("Attack")) return false; // Attack에서 빠져나가는 중
            }
            if (cur.IsName("Attack"))
            {
                normalizedTime = cur.normalizedTime;
                return true;
            }
        }

        return false;
    }

    private bool HasComboStates()
    {
        foreach (var h in stateHashes)
            if (!HasState(h)) return false;
        return true;
    }

    private bool HasState(int hash) => animator != null && animator.HasState(0, hash);

    private static float Get(float[] arr, int i, float fallback) =>
        arr != null && i >= 0 && i < arr.Length ? arr[i] : fallback;

    /// <summary>HammerHitbox가 실제로 적을 맞췄을 때 호출. 히트스톱 + 화면 흔들림.</summary>
    public void NotifyHitLanded()
    {
        bool finisher = ComboStep == comboStates.Length;

        if (cameraSwitch != null)
            cameraSwitch.Shake(hitShakeAmplitude * (finisher ? 2f : 1f), finisher ? 0.22f : 0.12f);

        if (hitStopDuration > 0f && !hitStopRunning && Time.timeScale > 0.5f)
            StartCoroutine(HitStop(hitStopDuration * (finisher ? finisherHitStopMultiplier : 1f)));
    }

    private IEnumerator HitStop(float duration)
    {
        hitStopRunning = true;
        const float slow = 0.05f;
        Time.timeScale = slow;
        yield return new WaitForSecondsRealtime(duration);
        // 그 사이 일시정지 메뉴 등이 timeScale을 바꿨다면 건드리지 않음
        if (Mathf.Approximately(Time.timeScale, slow))
            Time.timeScale = 1f;
        hitStopRunning = false;
    }

    private void OnDisable()
    {
        if (hitStopRunning && Time.timeScale < 0.5f && Time.timeScale > 0f)
            Time.timeScale = 1f;
        hitStopRunning = false;
    }

    public void EnableHammerCollider()
    {
        if (hammerCollider != null)
            hammerCollider.enabled = true;
    }

    public void DisableHammerCollider()
    {
        if (hammerCollider != null && hammerCollider.enabled)
            hammerCollider.enabled = false;
    }

    void TryHitChest()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, chestHitRange))
        {
            BreakableChest chest = hit.collider.GetComponentInParent<BreakableChest>();

            if (chest != null)
            {
                chest.TakeDamage(chestDamage);
            }
        }
    }
}
