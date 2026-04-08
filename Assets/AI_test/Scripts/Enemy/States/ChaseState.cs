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
        // 플레이어가 없으면 Idle로 복귀
        if (enemy.playerTransform == null)
        {
            enemy.TransitionToState(enemy.idleState);
            return;
        }

        // [핵심 수정] 사거리 체크 로직 고도화
        float dist = enemy.distanceToPlayer;
        float attackRange = enemy.stats.AttackRange;
        // (보스라면 패턴에 따라 사거리가 다를 수 있으니 주의)

        if (dist <= attackRange)
        {
            // A. 사거리 안으로 들어왔음!

            // 1. 일단 멈춘다 (제자리 걸음 방지!)
            enemy.navMeshAgent.isStopped = true;
            enemy.navMeshAgent.velocity = Vector3.zero; // 미끄러짐 방지

            // 2. 애니메이션을 Idle로 변경 (전투 대기 자세)
            // (isChasing을 끄면 Animator에서 Idle로 가게 연결되어 있어야 함)
            enemy.animator.SetBool("isMoving", false);

            // 3. 몸은 플레이어를 계속 쳐다본다 (등 보이면 바보 같으니까)
            LookAtPlayer(enemy);

            // 4. 공격 쿨타임 체크 -> 공격 가능하면 공격 상태로!
            if (enemy.attackTimer <= 0)
            {
                enemy.TransitionToState(enemy.attackState);
            }
            // (쿨타임 중이면 그냥 이 상태(멈춤+노려보기) 유지)
        }
        else
        {
            // B. 사거리 밖임 -> 열심히 쫓아가자!

            // 1. 다시 이동 시작
            enemy.navMeshAgent.isStopped = false;
            enemy.navMeshAgent.SetDestination(enemy.playerTransform.position);

            // 2. 애니메이션 Run
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