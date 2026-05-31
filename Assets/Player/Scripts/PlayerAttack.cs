using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    private int attackHash;

    public float attackLockTime = 1.1f;
    private float attackTimer = 0f;

    public bool IsAttacking { get; private set; }

    private PlayerInput playerInput;
    private PlayerRoll playerRoll;
    private MagicAttack magicAttack;

    private bool blockAttackUntilRelease = false;

    [Header("Hammer Hitbox")]
    [SerializeField] private Collider hammerCollider;

    [Header("Chest Hit")]
    public Camera playerCamera;
    public float chestHitRange = 5f;
    public int chestDamage = 1;

    private Coroutine hammerMonitorCoroutine;
    private bool hammerByAnimatorRunning = false;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        attackHash = Animator.StringToHash("Attack");

        playerInput = GetComponent<PlayerInput>();
        playerRoll = GetComponent<PlayerRoll>();
        magicAttack = GetComponent<MagicAttack>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (hammerCollider != null)
        {
            hammerCollider.enabled = false;
            Debug.Log("[PlayerAttack] Start - hammerCollider OFF");
        }
        else
        {
            Debug.LogWarning("[PlayerAttack] hammerCollider가 연결되지 않았습니다.");
        }
    }

    void Update()
    {
        if (playerInput == null) return;

        if (PauseMenuUI.IsPaused)
        {
            DisableHammerCollider();
            return;
        }

        if (StatsPanelToggle.UIBlocked)
        {
            DisableHammerCollider();
            return;
        }

        if (magicAttack != null && magicAttack.IsAiming)
        {
            DisableHammerCollider();
            return;
        }

        if (playerRoll != null && playerRoll.IsRolling)
        {
            blockAttackUntilRelease = true;
            DisableHammerCollider();
            Debug.Log("[PlayerAttack] Roll 중이라 공격 중지 / hammerCollider OFF");
            return;
        }

        if (blockAttackUntilRelease)
        {
            if (playerInput.IsAttackPressed)
                return;

            blockAttackUntilRelease = false;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            DisableHammerCollider();
            return;
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                IsAttacking = false;
                Debug.Log("[PlayerAttack] attackTimer 종료 - IsAttacking false");
            }

            return;
        }

        if (playerInput.IsAttackPressed)
        {
            if (animator != null)
                animator.SetTrigger(attackHash);

            attackTimer = attackLockTime;
            IsAttacking = true;

            Debug.Log("[PlayerAttack] 공격 시작 - Attack Trigger 실행");

            if (!hammerByAnimatorRunning)
            {
                if (hammerMonitorCoroutine != null)
                    StopCoroutine(hammerMonitorCoroutine);

                hammerMonitorCoroutine = StartCoroutine(MonitorHammerColliderByAttackAnimation());
            }

            TryHitChest();
        }
    }

    private IEnumerator MonitorHammerColliderByAttackAnimation()
    {
        hammerByAnimatorRunning = true;
        Debug.Log("[PlayerAttack] Attack 애니메이션 감시 시작");

        if (animator == null)
        {
            hammerByAnimatorRunning = false;
            yield break;
        }

        // Attack 상태 진입 대기
        float enterWait = 0f;
        while (!IsAttackState())
        {
            enterWait += Time.deltaTime;

            if (enterWait > 1.0f)
            {
                Debug.LogWarning("[PlayerAttack] Attack 상태 진입 실패 - 감시 종료");
                hammerByAnimatorRunning = false;
                yield break;
            }

            yield return null;
        }

        EnableHammerCollider();
        Debug.Log("[PlayerAttack] Attack 상태 진입 확인 - hammerCollider ON");

        // Attack 상태가 끝날 때까지 유지
        while (IsAttackState() || animator.IsInTransition(0))
        {
            yield return null;
        }

        DisableHammerCollider();
        IsAttacking = false;
        attackTimer = 0f;
        Debug.Log("[PlayerAttack] Attack 상태 종료 확인 - hammerCollider OFF, IsAttacking reset");

        hammerByAnimatorRunning = false;
        hammerMonitorCoroutine = null;
    }

    private bool IsAttackState()
    {
        if (animator == null)
            return false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Attack");
    }

    public void EnableHammerCollider()
    {
        if (hammerCollider != null)
        {
            hammerCollider.enabled = true;
            Debug.Log("[PlayerAttack] EnableHammerCollider() - hammerCollider ON");
        }
        else
        {
            Debug.LogWarning("[PlayerAttack] EnableHammerCollider() 실패 - hammerCollider 없음");
        }
    }

    public void DisableHammerCollider()
    {
        if (hammerCollider != null && hammerCollider.enabled)
        {
            hammerCollider.enabled = false;
            Debug.Log("[PlayerAttack] DisableHammerCollider() - hammerCollider OFF");
        }
    }

    void TryHitChest()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, chestHitRange))
        {
            Debug.Log("[PlayerAttack] Hit Object: " + hit.collider.name);

            BreakableChest chest = hit.collider.GetComponentInParent<BreakableChest>();

            if (chest != null)
            {
                chest.TakeDamage(chestDamage);
                Debug.Log("[PlayerAttack] BreakableChest 데미지 적용");
            }
        }
    }
}