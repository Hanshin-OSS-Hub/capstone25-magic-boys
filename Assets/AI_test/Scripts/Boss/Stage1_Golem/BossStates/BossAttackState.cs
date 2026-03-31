using UnityEngine;

public class BossAttackState : IBossState
{
    public void EnterState(BossStateManager boss)
    {
        // Debug.Log("보스: 공격 시작");
        boss.navMeshAgent.isStopped = true;

        // 플레이어 바라보기
        if (boss.playerTransform != null)
        {
            Vector3 targetPos = new Vector3(
                boss.playerTransform.position.x,
                boss.transform.position.y,
                boss.playerTransform.position.z
            );

            boss.transform.LookAt(targetPos);
        }

        // 2. 쿨타임 리셋
        boss.attackTimer = boss.stats.AttackCooldown;

        // 3. 공격 애니메이션 실행
        // (애니메이션의 마지막 프레임에 'OnBossAttackFinished' 이벤트가 있어야 함)
        boss.animator.SetTrigger("attack");
    }

    public void ExitState(BossStateManager boss)
    {
        boss.navMeshAgent.isStopped = false;
    }

    public void UpdateState(BossStateManager boss)
    {
        // 공격 중에도 플레이어를 계속 바라보게 하려면 아래 주석 해제
        // if (boss.playerTransform != null) boss.transform.LookAt(boss.playerTransform);
    }


}