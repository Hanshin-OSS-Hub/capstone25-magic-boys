using System.Collections;
using UnityEngine;

public class BossThrowState : IBossState
{
    public void EnterState(BossStateManager boss)
    {
        Debug.Log("보스: 바위 투척 준비!");
        boss.navMeshAgent.isStopped = true;

        if (boss.animator != null) boss.animator.SetTrigger("doThrow");

        // 1. 플레이어 바라보기
        if (boss.playerTransform != null) boss.transform.LookAt(boss.playerTransform);

        // 2. 투척 시퀀스 시작
        boss.StartCoroutine(ThrowRoutine(boss));
    }

    public void UpdateState(BossStateManager boss)
    {
        // 던지는 중에도 플레이어를 계속 바라봄
        if (boss.playerTransform != null) boss.transform.LookAt(boss.playerTransform);
    }

    public void ExitState(BossStateManager boss)
    {
        boss.navMeshAgent.isStopped = false;
    }

    private IEnumerator ThrowRoutine(BossStateManager boss)
    {
        float castTime = 1.0f;
        if (boss.stats is GolemData data) castTime = data.ThrowCastTime;

        // 1. 선딜레이 (돌을 집어 드는 시간)
        yield return new WaitForSeconds(castTime);

        // 2. 발사!
        PerformThrow(boss);

        // 3. 후딜레이 (던진 후 자세 복귀)
        yield return new WaitForSeconds(0.5f);

        // 4. 추적 복귀
        boss.TransitionToState(boss.chaseState);
    }

    private void PerformThrow(BossStateManager boss)
    {
        if (boss.stats is GolemData data && data.RockPrefab != null && boss.handTransform != null)
        {
            // 바위 생성
            GameObject rock = Object.Instantiate(data.RockPrefab, boss.handTransform.position, boss.handTransform.rotation);

            // 기존 EnemyProjectile 재활용
            EnemyProjectile proj = rock.GetComponent<EnemyProjectile>();
            if (proj != null)
            {
                proj.damage = data.ThrowDamage;
                proj.speed = data.ThrowSpeed;
                proj.lifeTime = 5.0f;
            }

            Debug.Log("보스: 바위 발사!");
        }
    }
}