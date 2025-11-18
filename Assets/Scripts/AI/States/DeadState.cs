using UnityEngine;
using UnityEngine.AI;

public class DeadState : IEnemyState
{
    public void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("에너미 사망");
        enemy.NavMeshAgent.isStopped = true; // 이동 비활성화
        enemy.NavMeshAgent.enabled = false; // 땅 고정 비활성화
        enemy.StartDeathSequence();
        enemy.GetComponent<Collider>().enabled = false;
    }

    public void ExitState(EnemyStateManager enemy)
    {

    }

    public void UpdateState(EnemyStateManager enemy)
    {
        
    }


}
