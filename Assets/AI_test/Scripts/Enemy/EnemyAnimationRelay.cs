using UnityEngine;

public class EnemyAnimationRelay : MonoBehaviour
{
    private EnemyStateManager parentEnemy;

    void Awake()
    {
        // 내 부모님(EnemyStateManager)을 찾아서 기억해둠
        parentEnemy = GetComponentInParent<EnemyStateManager>();
    }

    // 애니메이션 이벤트에서 이 함수를 부를 겁니다.
    public void PerformAttack()
    {
        if (parentEnemy != null) parentEnemy.PerformAttack();
    }

    // 공격 애니메이션이 끝날 때 부를 함수
    public void OnAttackAnimationFinished()
    {
        if (parentEnemy != null) parentEnemy.OnAttackAnimationFinished();
    }
}