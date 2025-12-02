using UnityEngine;

public class BossChaseState : IBossState
{
    public void EnterState(BossStateManager boss)
    {
        boss.navMeshAgent.isStopped = false;
        if (boss.animator != null) boss.animator.SetBool("isMoving", true);
    }

    public void ExitState(BossStateManager boss)
    {
        boss.navMeshAgent.isStopped = true;
        if (boss.animator != null) boss.animator.SetBool("isMoving", false);
    }

    public void UpdateState(BossStateManager boss)
    {
        if (boss.playerTransform == null) return;

        float distance = Vector3.Distance(boss.transform.position, boss.playerTransform.position);

        // ================================================================
        // 1. [근거리 상황] 공격 사거리(AttackRange) 안에 들어왔을 때
        // ================================================================
        if (distance <= boss.stats.AttackRange && boss.attackTimer <= 0)
        {
            int dice = Random.Range(0, 100);

            // [30% 확률] 대지 분쇄 (Smash)
            if (dice < 30 && boss.stats is GolemData && boss.smashCooldownTimer <= 0)
            {
                boss.TransitionToState(boss.SmashState);
            }
            // [70% 확률] 기본 평타
            else
            {
                boss.TransitionToState(boss.attackState);
            }
            return;
        }

        // ================================================================
        // 2. [중거리 상황] 사거리보다는 멀고, 15m 보다는 가까울 때
        // ================================================================
        else if (distance > boss.stats.AttackRange && distance < 15f && boss.attackTimer <= 0)
        {
            int dice = Random.Range(0, 100);

            // [40% 확률] 돌진 (Rush)
            if (dice < 40 && boss.stats is GolemData && boss.rushCooldownTimer <= 0)
            {
                boss.TransitionToState(boss.RushState);
            }
            // [20% 확률] 바위 투척 (Throw)
            else if (dice < 60 && boss.throwCooldownTimer <= 0)
            {
                boss.TransitionToState(boss.throwState);

                // 투척 쿨타임 리셋
                if (boss.stats is GolemData data)
                    boss.throwCooldownTimer = data.ThrowCooldown;
                else
                    boss.throwCooldownTimer = 8f;
            }
            // [나머지 40%] 그냥 계속 쫓아감(아래 쫓아가는 코드 실행, return 하면 안됨)
        }

        // ================================================================
        // 3. 이동 (추적)
        // ================================================================
        if (boss.navMeshAgent.isOnNavMesh)
        {
            boss.navMeshAgent.SetDestination(boss.playerTransform.position);
        }
    }
}