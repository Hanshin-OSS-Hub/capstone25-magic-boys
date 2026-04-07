using UnityEngine;

public class OverloadState : IBossState
{
    private float stunTimer;

    public void EnterState(BossStateManager boss)
    {
        Debug.Log("보스: 시스템 과부하! (스턴)");

        // 1. 애니메이션 시작 (isOverloaded 켜기)
        boss.animator.SetBool("isOverloaded", true);

        boss.navMeshAgent.isStopped = true;
        boss.navMeshAgent.velocity = Vector3.zero;

        if (boss.stats is GolemData data)
        {
            stunTimer = data.OverloadDuration;
        }
        else
        {
            stunTimer = 5f;
        }
    }

    public void UpdateState(BossStateManager boss)
    {
        stunTimer -= Time.deltaTime;

        if (stunTimer <= 0)
        {
            boss.TransitionToState(boss.chaseState);
        }
    }

    public void ExitState(BossStateManager boss)
    {
        // 2. 애니메이션 종료 (isOverloaded 끄기)
        boss.animator.SetBool("isOverloaded", false);

        boss.navMeshAgent.isStopped = false;

        if (boss.stats is GolemData data)
        {
            boss.heatTimer = data.OverheatInterval;
        }
        else
        {
            boss.heatTimer = 10f;
        }
    }
}