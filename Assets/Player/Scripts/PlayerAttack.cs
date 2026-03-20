using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    private int attackHash;

    public float attackLockTime = 1.1f;
    private float attackTimer = 0f;

    public bool IsAttacking { get; private set; }

    private PlayerInput playerInput;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        attackHash = Animator.StringToHash("Attack");
        playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        if (playerInput == null) return;

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
        }
    }
}