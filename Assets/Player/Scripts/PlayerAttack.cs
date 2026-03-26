using UnityEngine;
using UnityEngine.EventSystems;
using StarterAssets;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    private int attackHash;
    private int airAttackHash;

    public float attackLockTime = 1.1f;
    private float attackTimer = 0f;

    public bool IsAttacking { get; private set; }
    public bool IsAirAttacking { get; private set; }

    private PlayerInput playerInput;
    private ThirdPersonController controller;

    [Header("Chest Hit")]
    public Camera playerCamera;
    public float chestHitRange = 5f;
    public int chestDamage = 1;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        attackHash = Animator.StringToHash("Attack");
        airAttackHash = Animator.StringToHash("AirAttack");

        playerInput = GetComponent<PlayerInput>();
        controller = GetComponent<ThirdPersonController>();

        if (!playerCamera)
            playerCamera = Camera.main;
    }

    void Update()
    {
        if (playerInput == null || controller == null) return;

        if (StatsPanelToggle.UIBlocked) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                IsAttacking = false;
                IsAirAttacking = false;
            }

            return;
        }

        if (playerInput.IsAttackPressed)
        {
            if (controller.Grounded)
            {
                animator.SetTrigger(attackHash);
                IsAttacking = true;
                IsAirAttacking = false;

                TryHitChest();
            }
            else
            {
                animator.SetTrigger(airAttackHash);
                IsAirAttacking = true;
                IsAttacking = false;
            }

            attackTimer = attackLockTime;
        }
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