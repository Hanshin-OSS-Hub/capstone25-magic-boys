using System.Collections;
using UnityEngine;

public class BossThrowState : IBossState
{
    public void EnterState(BossStateManager boss)
    {
        Debug.Log("보스: 바위 투척 준비!");
        boss.navMeshAgent.isStopped = true;

        // 1. 애니메이션 재생
        if (boss.animator != null)
        {
            boss.animator.SetTrigger("doThrow");
        }

        // 2. 플레이어 바라보기 (초기화)
        LookAtPlayerFlat(boss);

        // 3. 투척 시퀀스 코루틴 시작
        boss.StartCoroutine(ThrowRoutine(boss));
    }

    public void UpdateState(BossStateManager boss)
    {
        // 4. 투척 준비 동작 중에도 몸통은 계속 플레이어를 향해 회전 (Y축 고정)
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
                boss.transform.position.y, // 높이는 보스 자신의 높이로 고정
                boss.playerTransform.position.z
            );
            boss.transform.LookAt(targetPos);
        }
    }

    private IEnumerator ThrowRoutine(BossStateManager boss)
    {
        // 데이터 가져오기
        float castTime = 1.0f;
        if (boss.stats is GolemData data) castTime = data.ThrowCastTime;

        // 1. 선딜레이 (돌을 집어 드는 시간)
        yield return new WaitForSeconds(castTime);

        // 2. 발사!
        PerformThrow(boss);

        // 3. 후딜레이 (던진 후 자세 복귀 - 애니메이션 길이에 맞춰 조절)
        yield return new WaitForSeconds(1.0f);

        // 4. 추적 복귀
        boss.TransitionToState(boss.chaseState);
    }

    private void PerformThrow(BossStateManager boss)
    {
        // 데이터 및 필수 컴포넌트 확인
        if (boss.stats is GolemData data && data.RockPrefab != null && boss.handTransform != null)
        {
            // ▼▼▼ [핵심] 발사 각도(회전)를 따로 계산! ▼▼▼

            // 1. 손(FirePoint) 위치에서 플레이어의 '가슴' 쪽을 향하는 방향 벡터 계산
            // (플레이어 위치 + 위로 1m 정도를 노려야 바닥이 아니라 몸을 맞춥니다)
            Vector3 targetPoint = Vector3.zero;

            if (boss.playerTransform != null)
            {
                targetPoint = boss.playerTransform.position + Vector3.up * 1.0f;
            }
            else
            {
                // 플레이어가 없으면 그냥 정면으로
                targetPoint = boss.handTransform.position + boss.transform.forward;
            }

            Vector3 aimDir = (targetPoint - boss.handTransform.position).normalized;

            // 2. 그 방향을 바라보는 회전값(Quaternion) 생성
            Quaternion lookRot = Quaternion.LookRotation(aimDir);

            // 3. 바위 생성 (손의 회전값 대신, 계산한 lookRot 사용)
            GameObject rock = Object.Instantiate(data.RockPrefab, boss.handTransform.position, lookRot);

            // 4. 투사체 속성 설정
            EnemyProjectile proj = rock.GetComponent<EnemyProjectile>();
            if (proj != null)
            {
                proj.damage = data.ThrowDamage;
                proj.speed = data.ThrowSpeed;
                proj.lifeTime = 5.0f; // 5초 뒤 삭제
            }

            Debug.Log("보스: 바위 발사! (플레이어 정밀 조준)");
        }
    }
}