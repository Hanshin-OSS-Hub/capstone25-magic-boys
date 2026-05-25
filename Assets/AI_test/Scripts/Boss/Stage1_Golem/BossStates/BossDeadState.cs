using UnityEngine;

public class BossDeadState : IBossState
{
    public void EnterState(BossStateManager boss)
    {
        Debug.Log("보스 사망!");

        boss.navMeshAgent.isStopped = true;
        boss.navMeshAgent.enabled = false; // 땅 고정 해제 (가라앉는 애니메이션을 위해)
        boss.GetComponent<Collider>().enabled = false;

        //Has Exit Time 이슈로 애니메이션 전환 대신 강제로 재생하도록 변경
        //if (boss.animator != null) boss.animator.SetTrigger("dead");
        if (boss.animator != null)
        {
            boss.animator.Play("Dead");
        }

        if (boss.deadSound != null || boss.deadSound2 != null)
        {
            SoundManager.Instance.PlaySFX3D(boss.deadSound, boss.transform.position);
            SoundManager.Instance.PlaySFX3D(boss.deadSound2, boss.transform.position);
        }

        if (boss.weakPointObject != null) boss.weakPointObject.SetActive(false);

        boss.StartDeathSequence();

        if (boss.portalObject != null)
        {
            boss.portalObject.SetActive(true);
            Debug.Log("Portal 생성 완료");
        }

        boss.enabled = false;
    }

    public void UpdateState(BossStateManager boss) { }
    public void ExitState(BossStateManager boss) { }
}