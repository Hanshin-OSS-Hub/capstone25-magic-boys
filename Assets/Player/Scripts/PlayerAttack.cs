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
    private MagicAttack magicAttack;

    [Header("Chest Hit")]
    public Camera playerCamera;
    public float chestHitRange = 5f;
    public int chestDamage = 1;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        attackHash = Animator.StringToHash("Attack");
        playerInput = GetComponent<PlayerInput>();
        magicAttack = GetComponent<MagicAttack>();

        if (!playerCamera)
            playerCamera = Camera.main;
    }

    void Update()
    {
        if (playerInput == null) return;

        if (StatsPanelToggle.UIBlocked) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Skip normal attack if a magic skill is currently being prepared/aimed
        if (magicAttack != null && magicAttack.IsSkillPrepared)
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
            Debug.Log("���� ������Ʈ: " + hit.collider.name);

            BreakableChest chest = hit.collider.GetComponentInParent<BreakableChest>();
            if (chest != null)
            {
                chest.TakeDamage(chestDamage);
            }
        }
    }
}