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

    // 추가: 구르기 끝난 직후 남아있는 공격 입력 무시용
    private bool blockAttackUntilRelease = false;

    [Header("Chest Hit")]
    public Camera playerCamera;
    public float chestHitRange = 5f;
    public int chestDamage = 1;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        attackHash = Animator.StringToHash("Attack");

        playerInput = GetComponent<PlayerInput>();
        playerRoll = GetComponent<PlayerRoll>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        if (playerInput == null) return;

        // 구르는 중에는 공격 막기
        if (playerRoll != null && playerRoll.IsRolling)
        {
            blockAttackUntilRelease = true;   // 추가
            return;
        }

        // 구르기 끝난 뒤 공격 버튼이 아직 눌려있으면 공격 안 함
        if (blockAttackUntilRelease)
        {
            if (playerInput.IsAttackPressed)
                return;

            blockAttackUntilRelease = false;
        }

        if (StatsPanelToggle.UIBlocked) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
                IsAttacking = false;
            return;
        }

        if (playerInput.IsAttackPressed)
        {
            animator.SetTrigger(attackHash);
            attackTimer = attackLockTime;
            IsAttacking = true;

            TryHitChest();
        }
    }

    void TryHitChest()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, chestHitRange))
        {
            Debug.Log("맞은 오브젝트: " + hit.collider.name);

            BreakableChest chest = hit.collider.GetComponentInParent<BreakableChest>();
            if (chest != null)
            {
                chest.TakeDamage(chestDamage);
            }
        }
    }
}