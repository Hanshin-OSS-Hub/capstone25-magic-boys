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

        float dist = Vector3.Distance(boss.transform.position, boss.playerTransform.position);

        float smashRange = 3.0f;
        float attackRange = 2.5f;

        if (boss.stats is GolemData data)
        {
            smashRange = data.SmashRadius;
            attackRange = data.AttackRange;
        }

        // 1. [근거리/초근접] 일단 멈춤
        // 가까이 붙었으면 굳이 더 밀지 말고 멈춰서 쿨타임(평타/스매시) 기다림
        if (dist <= smashRange || dist <= attackRange)
        {
            StopAndLook(boss);
            boss.SelectNextPattern();
        }
        // 2. [중거리] 조건부 정지
        else if (dist <= 15.0f)
        {
            // 스킬을 쓸 수 있으면 쓰고(true), 못 쓰면(false) 계속 쫓아감
            bool skillUsed = boss.SelectNextPattern();

            if (skillUsed)
            {
            }
            else
            {
                ChasePlayer(boss);
            }
        }
        // 3. [원거리] 계속 추적
        else
        {
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

    // [전투 대기 상태]
    private void StopAndLook(BossStateManager boss)
    {
        // 1. 물리적 이동 멈춤
        boss.navMeshAgent.isStopped = true;
        boss.navMeshAgent.velocity = Vector3.zero;

        // 2. [수정] 애니메이션 멈춤 (Idle로 전환)
        // isMoving을 false로 꺼야 Idle 상태로 돌아감
        boss.animator.SetBool("isMoving", false);

        // 3. 플레이어 바라보기
        Vector3 dir = (boss.playerTransform.position - boss.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, lookRot, Time.deltaTime * 5f);
        }
    }
}