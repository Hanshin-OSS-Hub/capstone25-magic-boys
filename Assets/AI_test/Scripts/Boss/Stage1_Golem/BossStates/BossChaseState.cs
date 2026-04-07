using UnityEngine;

public class BossChaseState : IBossState
{
    public void EnterState(BossStateManager boss)
    {
        boss.animator.SetBool("isMoving", true);
        boss.navMeshAgent.isStopped = false;
    }

    public void UpdateState(BossStateManager boss)
    {
        if (boss.playerTransform == null) return;

        // 1. 가장 먼저 스킬(패턴) 사용 가능 여부 체크
        // (거리 상관없이 쿨타임 찼으면 스킬 쓰러 감 -> true 반환 시 종료)
        if (boss.SelectNextPattern())
        {
            return;
        }

        // 2. 스킬 쿨타임 or 스킬 사거리 외  -> 평타

        float dist = Vector3.Distance(boss.transform.position, boss.playerTransform.position);
        float attackRange = 2.5f;
        if (boss.stats is GolemData data) attackRange = data.AttackRange;

        // 3. 평타 사거리 이내
        if (dist <= attackRange)
        {
            StopAndLook(boss);

            // 평타 쿨타임은 SelectNextPattern 안에서 체크했으므로,
            // 여기까지 왔다는 건 평타 쿨타임도 안 찼다는 뜻임.
            // 노려보기 상태 유지
        }
        else
        {
            // 사거리 밖이면 계속 추적 (스매쉬 범위여도 멈추지 않음)
            ChasePlayer(boss);
        }
    }

    private void ChasePlayer(BossStateManager boss)
    {
        boss.navMeshAgent.isStopped = false;
        boss.navMeshAgent.SetDestination(boss.playerTransform.position);

        boss.animator.SetBool("isMoving", true);
    }

    public void ExitState(BossStateManager boss)
    {
        boss.navMeshAgent.isStopped = true;
        boss.animator.SetBool("isMoving", false);
    }

    private void StopAndLook(BossStateManager boss)
    {
        boss.navMeshAgent.isStopped = true;
        boss.navMeshAgent.velocity = Vector3.zero;
        boss.animator.SetBool("isMoving", false);

        Vector3 dir = (boss.playerTransform.position - boss.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, lookRot, Time.deltaTime * 5f);
        }
    }
}