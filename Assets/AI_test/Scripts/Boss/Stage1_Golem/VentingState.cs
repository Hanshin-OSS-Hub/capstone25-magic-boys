using UnityEngine;

public class VentingState : IBossState
{
    private float timer;

    public void EnterState(BossStateManager boss)
    {
        Debug.Log("보스: 과열! 냉각 모드 (약점 노출)");
        boss.navMeshAgent.isStopped = true;

        // 약점 활성화
        if (boss.weakPointObject != null) boss.weakPointObject.SetActive(true);

        if (boss.stats is GolemData data)
        {
            timer = data.VentingDuration;
        }
        else
        {
            timer = 5f; // 기본값 초기화
        }
        
    }

    public void UpdateState(BossStateManager boss)
    {
        timer -= Time.deltaTime;

        // 시간이 다 되면 다시 추적 상태로
        if (timer <= 0)
        {
            boss.TransitionToState(boss.chaseState);
        }
    }

    public void ExitState(BossStateManager boss)
    {
        if (boss.weakPointObject != null) boss.weakPointObject.SetActive(false);
        boss.navMeshAgent.isStopped = false;

        // 타이머 리셋 형변환
        if (boss.stats is GolemData data)
        {
            boss.heatTimer = data.OverheatInterval;
        }
        else
        {
            boss.heatTimer = 10f; // 기본값 초기화
        }
        
    }
}