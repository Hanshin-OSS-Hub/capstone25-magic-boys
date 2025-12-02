using UnityEngine;

public class IdleState : IEnemyState
{
    public void EnterState(EnemyStateManager enemy)
    {
        //Debug.Log("대기 시작!");
        enemy.navMeshAgent.isStopped = true;
    }

    public void ExitState(EnemyStateManager enemy)
    {
        //Debug.Log("대기 종료.");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        // 플레이어가 인식 범위 내에 들어왔는가?
        if (enemy.distanceToPlayer <= enemy.stats.DetectionRange)
        {
            enemy.TransitionToState(enemy.chaseState);
            return;
        }

    }
}