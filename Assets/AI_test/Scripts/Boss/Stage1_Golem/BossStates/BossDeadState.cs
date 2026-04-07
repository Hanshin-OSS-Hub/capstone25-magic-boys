using UnityEngine;

public class BossDeadState : IBossState
{
    public void EnterState(BossStateManager boss)
    {
        Debug.Log("보스 사망!");

        // 1. 이동 및 기능 정지
        boss.navMeshAgent.isStopped = true;
        boss.navMeshAgent.enabled = false; // 땅 고정 해제 (가라앉는 애니메이션을 위해)
        boss.GetComponent<Collider>().enabled = false;

        if (boss.animator != null) boss.animator.SetTrigger("dead");

        // 3. 약점 숨기기 (혹시 켜져있다면)
        if (boss.weakPointObject != null) boss.weakPointObject.SetActive(false);

        // 4. 죽음 시퀀스 시작
        boss.StartDeathSequence();

        // 5. 스크립트 비활성화
        boss.enabled = false;
    }

    public void UpdateState(BossStateManager boss) { }
    public void ExitState(BossStateManager boss) { }
}