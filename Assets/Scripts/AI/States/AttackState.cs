using UnityEngine;

public class AttackState : IEnemyState
{
    private float attackAnimationTimer; // 애니메이션이 없는 현재, 임시로 쓸 공격 애니메이션 타이머

    public void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("공격 시작!");
        enemy.NavMeshAgent.isStopped = true;
        enemy.transform.LookAt(enemy.PlayerTransform);
        enemy.attackTimer = enemy.GetAttackCooldown();

        attackAnimationTimer = 1.0f; // 임시: 1초 동안 공격 애니메이션 재생
    }

    public void ExitState(EnemyStateManager enemy)
    {
        Debug.Log("공격 종료.");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        enemy.transform.LookAt(enemy.PlayerTransform);

        attackAnimationTimer -= Time.deltaTime;
        if (attackAnimationTimer <= 0) 
        { 
            enemy.OnAttackAnimationFinished(); 
        }




    }

}
