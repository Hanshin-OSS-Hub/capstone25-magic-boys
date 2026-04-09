using UnityEngine;

public class VentingState : IBossState
{
    private float timer;

    public void EnterState(BossStateManager boss)
    {
        Debug.Log("보스: 과열! 냉각 모드 (약점 노출)");

        boss.animator.SetBool("isVenting", true);

        boss.navMeshAgent.isStopped = true;
        boss.navMeshAgent.velocity = Vector3.zero; // 미끄러짐 방지

        // 약점 활성화
        if (boss.weakPointObject != null) boss.weakPointObject.SetActive(true);

        if (boss.stats is GolemData data)
        {
            timer = data.VentingDuration;
        }
        else
        {
            timer = 5f;
        }
    }

    public void UpdateState(BossStateManager boss)
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            boss.TransitionToState(boss.chaseState);
        }
    }

    public void ExitState(BossStateManager boss)
    {
        boss.animator.SetBool("isVenting", false);

        if (boss.ventingSound != null)
        {
            SoundManager.Instance.PlaySFX3D(boss.ventingSound, boss.transform.position);
        }

        if (boss.weakPointObject != null) boss.weakPointObject.SetActive(false);
        boss.navMeshAgent.isStopped = false;

        // 타이머 리셋
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