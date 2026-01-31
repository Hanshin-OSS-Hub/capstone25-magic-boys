using UnityEngine;

public class AttackState : IEnemyState
{
    public void EnterState(EnemyStateManager enemy)
    {
        // 1. 애니메이터에게 공격 신호 보내기!
        enemy.animator.SetTrigger("attack");

        enemy.navMeshAgent.isStopped = true;
        enemy.transform.LookAt(enemy.playerTransform);

        enemy.attackTimer = enemy.stats.AttackCooldown;
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        // 플레이어 응시
        if (enemy.playerTransform != null)
        {
            Vector3 dir = enemy.playerTransform.position - enemy.transform.position;
            dir.y = 0; // 기울어짐 방지
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }

        // 상태 종료는 애니메이션이 끝날 때 Event에서 호출
    }

    public void ExitState(EnemyStateManager enemy)
    {
        // 나갈 때 필요한 로직이 있다면 작성
    }

}