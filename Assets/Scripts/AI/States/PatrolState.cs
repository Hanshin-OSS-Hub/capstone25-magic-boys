using UnityEngine;

public class PatrolState : IEnemyState
{
    public void EnterState(EnemyStateManager enemy)
    {
        //Debug.Log("순찰 시작!");
        enemy.NavMeshAgent.isStopped = false;
        if (enemy.GetPatrolPoints().Length > 0)
        {
            // 순찰 지점으로 이동
            enemy.NavMeshAgent.SetDestination(enemy.GetPatrolPoints()[enemy.currentPatrolIndex].position);
        }
    }

    public void ExitState(EnemyStateManager enemy)
    {
        //Debug.Log("순찰 종료.");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        if (enemy.DistanceToPlayer <= enemy.GetDetectionRange())
        {
            enemy.TransitionToState(enemy.chaseState);
            return;
        }


        if (enemy.GetPatrolPoints().Length > 0) { 
        // 경로탐색완료 && 남은 거리 < 0.5f, 경로간 이동이 이상하면 거리를 좀 더 늘려볼 것 
            if (!enemy.NavMeshAgent.pathPending && enemy.NavMeshAgent.remainingDistance < 0.5f)
            {
                // 다음 순찰 지점으로 이동, 나머지 연산으로 배열 초과 방지
                enemy.currentPatrolIndex = (enemy.currentPatrolIndex + 1) % enemy.GetPatrolPoints().Length;
                enemy.NavMeshAgent.SetDestination(enemy.GetPatrolPoints()[enemy.currentPatrolIndex].position);
            }
         }


    }

}
