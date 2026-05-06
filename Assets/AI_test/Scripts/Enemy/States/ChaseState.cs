using UnityEngine;

public class ChaseState : IEnemyState
{
    public void EnterState(EnemyStateManager enemy)
    {
        enemy.animator.SetBool("isMoving", true);
        enemy.navMeshAgent.isStopped = false;
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        if (enemy.playerTransform == null)
        {
            enemy.TransitionToState(enemy.idleState);
            return;
        }

        float dist = enemy.distanceToPlayer;
        float attackRange = enemy.stats.AttackRange;

        if (dist <= attackRange)
        {
            enemy.navMeshAgent.isStopped = true;
            enemy.navMeshAgent.velocity = Vector3.zero; // 미끄러짐 방지

            enemy.animator.SetBool("isMoving", false);

            LookAtPlayer(enemy);

            if (enemy.attackTimer <= 0)
            {
                enemy.TransitionToState(enemy.attackState);
            }
        }
        else
        {
            enemy.navMeshAgent.isStopped = false;
            enemy.navMeshAgent.SetDestination(enemy.playerTransform.position);

            enemy.animator.SetBool("isMoving", true);
        }
    }

    public void ExitState(EnemyStateManager enemy)
    {
        enemy.animator.SetBool("isMoving", false);
        enemy.navMeshAgent.isStopped = true;
    }

    // 플레이어 바라보기
    private void LookAtPlayer(EnemyStateManager enemy)
    {
        Vector3 dir = (enemy.playerTransform.position - enemy.transform.position).normalized;
        dir.y = 0; // 높이 무시 (기울어짐 방지)
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, lookRot, Time.deltaTime * 10f);
        }
    }
}