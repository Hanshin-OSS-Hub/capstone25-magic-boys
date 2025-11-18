using UnityEngine;

public class ChaseState : IEnemyState
{
    public void EnterState(EnemyStateManager enemy)
    {
        //Debug.Log("추격 시작!");
        enemy.NavMeshAgent.isStopped = false;
    }

    public void ExitState(EnemyStateManager enemy)
    {
        //Debug.Log("추격 종료.");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        // 공격 할 수 있는가?
        if (enemy.DistanceToPlayer <= enemy.GetAttackRange() && enemy.attackTimer <= 0)
        {
            enemy.TransitionToState(enemy.attackState);
            return;
        }
        // 플레이어를 놓쳤는가?
        if (enemy.DistanceToPlayer > enemy.GetDetectionRange()) 
        {
            enemy.TransitionToState(enemy.patrolState);
            return;
        }
        enemy.NavMeshAgent.SetDestination(enemy.PlayerTransform.position); // 플레이어의 좌표를 목표지점으로 설정
    }


}
