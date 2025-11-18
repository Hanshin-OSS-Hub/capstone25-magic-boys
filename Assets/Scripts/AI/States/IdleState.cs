using UnityEngine;

public class IdleState : IEnemyState
{
    private float idleTimer; // 대기 시간 타이머
    public void EnterState(EnemyStateManager enemy)
    {
        //Debug.Log("대기 시작!");
        enemy.NavMeshAgent.isStopped = true;
        idleTimer = 3f; // 3초 대기
        
    }

    public void ExitState(EnemyStateManager enemy)
    {
        //Debug.Log("대기 종료.");
    }

    public void UpdateState(EnemyStateManager enemy)
    {  
        if (enemy.DistanceToPlayer <= enemy.GetDetectionRange())
        {
            enemy.TransitionToState(enemy.chaseState);
            return;
        }

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0) { 
            enemy.TransitionToState(enemy.patrolState);
            return;
        }
    }



}
