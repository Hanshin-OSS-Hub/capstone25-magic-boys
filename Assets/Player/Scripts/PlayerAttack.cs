using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;
    private int attackHash;

    public float attackLockTime = 1.8f;
    private float attackTimer = 0f;

    //  이동을 막기 위한 플래그
    public bool IsAttacking { get; private set; }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        attackHash = Animator.StringToHash("Attack");
    }

    void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
                IsAttacking = false;

            return; // 공격 중일 때 다른 입력 무시
        }

        if (Input.GetMouseButtonDown(0))
        {
            animator.SetTrigger(attackHash);
            attackTimer = attackLockTime;

            //  공격 시작
            IsAttacking = true;
        }
    }
}
