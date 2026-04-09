using System.Collections;
using UnityEngine;

public class BossThrowState : IBossState
{
    public void EnterState(BossStateManager boss)
    {
        Debug.Log("보스: 바위 투척 준비!");
        boss.navMeshAgent.isStopped = true;

        if (boss.animator != null)
        {
            boss.animator.SetTrigger("doThrow");
        }

        LookAtPlayerFlat(boss);

        boss.StartCoroutine(ThrowRoutine(boss));
    }

    public void UpdateState(BossStateManager boss)
    {
        // 투척 준비 동작 중에도 몸통은 계속 플레이어를 향해 회전
        LookAtPlayerFlat(boss);
    }

    public void ExitState(BossStateManager boss)
    {
        boss.navMeshAgent.isStopped = false;
    }

    // 보스가 고개를 숙이지 않고(버벅거림 방지) 수평으로만 회전하게 하는 함수
    private void LookAtPlayerFlat(BossStateManager boss)
    {
        if (boss.playerTransform != null)
        {
            Vector3 targetPos = new Vector3(
                boss.playerTransform.position.x,
                boss.transform.position.y,
                boss.playerTransform.position.z
            );
            boss.transform.LookAt(targetPos);
        }
    }

    private IEnumerator ThrowRoutine(BossStateManager boss)
    {
        float castTime = 1.0f;
        if (boss.stats is GolemData data) castTime = data.ThrowCastTime;

        // 선딜레이 (돌을 집어 드는 시간)
        yield return new WaitForSeconds(castTime);
        PerformThrow(boss);
        // 후딜레이 (던진 후 자세 복귀 - 애니메이션 길이에 맞춰 조절)
        yield return new WaitForSeconds(1.0f);

        boss.TransitionToState(boss.chaseState);
    }

    private void PerformThrow(BossStateManager boss)
    {
        if (boss.stats is GolemData data && data.RockPrefab != null && boss.handTransform != null)
        {

            Vector3 targetPoint = Vector3.zero;

            if (boss.playerTransform != null)
            {
                targetPoint = boss.playerTransform.position + Vector3.up * 1.0f;
            }
            else
            {
                targetPoint = boss.handTransform.position + boss.transform.forward;
            }

            Vector3 aimDir = (targetPoint - boss.handTransform.position).normalized;

            Quaternion lookRot = Quaternion.LookRotation(aimDir);

            GameObject rock = Object.Instantiate(data.RockPrefab, boss.handTransform.position, lookRot);

            EnemyProjectile proj = rock.GetComponent<EnemyProjectile>();
            if (proj != null)
            {
                proj.damage = data.ThrowDamage;
                proj.speed = data.ThrowSpeed;
                proj.lifeTime = 5.0f; // 5초 뒤 삭제
            }
            if (boss.throwSound != null)
            {
                SoundManager.Instance.PlaySFX3D(boss.throwSound, boss.transform.position);
            }

        }
    }
}