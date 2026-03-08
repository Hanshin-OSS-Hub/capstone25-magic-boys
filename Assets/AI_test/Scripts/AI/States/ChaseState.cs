using UnityEngine;

public class ChaseState : IEnemyState
{
    public void EnterState(EnemyStateManager enemy)
    {
        //Debug.Log("추격 시작!");
        enemy.navMeshAgent.isStopped = false;
    }

    public void ExitState(EnemyStateManager enemy)
    {
        //Debug.Log("추격 종료.");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        // 1. 공격 가능? (이건 유지해야 함)
        if (enemy.distanceToPlayer <= enemy.stats.AttackRange && enemy.attackTimer <= 0)
        {
            enemy.TransitionToState(enemy.attackState);
            return;
        }

        // 인식 범위를 벗어나면 대기 상태로 전환
        //if (enemy.distanceToPlayer > enemy.stats.DetectionRange) 
        //{
        //    enemy.TransitionToState(enemy.idleState);
        //    return;
        //}


        if (enemy.playerTransform != null)
        {
            enemy.navMeshAgent.SetDestination(enemy.playerTransform.position);
        }
    }
}