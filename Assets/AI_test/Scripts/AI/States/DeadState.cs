using UnityEngine;
using UnityEngine.AI;

public class DeadState : IEnemyState
{
    public void EnterState(EnemyStateManager enemy)
    {
        enemy.animator.SetTrigger("dead");
        enemy.navMeshAgent.isStopped = true; // 이동 비활성화
        enemy.navMeshAgent.velocity = Vector3.zero;

        Collider col = enemy.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        enemy.StartDeathSequence();
    }

    public void ExitState(EnemyStateManager enemy)
    {

    }

    public void UpdateState(EnemyStateManager enemy)
    {
        
    }


}
