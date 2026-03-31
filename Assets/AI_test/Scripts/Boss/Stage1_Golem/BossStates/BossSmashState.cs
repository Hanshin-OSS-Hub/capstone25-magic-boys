using UnityEngine;
using UnityEngine.UI;

public class BossSmashState : IBossState
{
    private float timer;
    private float castTime;
    private bool hasSmashed;

    private RectTransform fillRect; // 장판 크기 조절용
    private GolemData data;

    private float recoveryTime = 1.5f; // 스매시 데미지 판정 후, 애니메이션이 끝날 때까지 대기할 시간 (후딜레이)

    public void EnterState(BossStateManager boss)
    {
        boss.navMeshAgent.isStopped = true;
        boss.navMeshAgent.velocity = Vector3.zero;

        hasSmashed = false;
        if (boss.animator != null) boss.animator.SetTrigger("doSmash");

        // 데이터 가져오기 (형변환 후 저장)
        data = boss.stats as GolemData;

        castTime = (data != null) ? data.SmashCastTime : 1.5f;
        timer = castTime;

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

        // 장판 켜기 및 초기화
        if (boss.smashIndicator != null)
        {
            boss.smashIndicator.SetActive(true);

            Transform fillObj = boss.smashIndicator.transform.Find("FillImage");

            if (fillObj != null)
            {
                fillRect = fillObj.GetComponent<RectTransform>();
                if (fillRect != null) fillRect.localScale = Vector3.zero; // 크기 0에서 시작
            }
        }
    }

    public void UpdateState(BossStateManager boss)
    {
        timer -= Time.deltaTime;

        if (!hasSmashed)
        {
            // [페이즈 1] 선딜레이: 장판이 중심에서 밖으로 커지는 연출
            if (fillRect != null && castTime > 0)
            {
                float progress = Mathf.Clamp01((castTime - timer) / castTime);
                fillRect.localScale = Vector3.one * progress;
            }

            // 시간이 다 됐고, 아직 공격 안 했으면 데미지 판정 실행
            if (timer <= 0)
            {
                PerformSmash(boss);
                hasSmashed = true;

                timer = recoveryTime;
            }
        }
        else
        {
            // [페이즈 2] 후딜레이: 공격 후 자세를 되찾는 동안 대기
            if (timer <= 0)
            {
                // 후딜레이까지 모두 끝나야 비로소 추적 상태로 복귀
                boss.TransitionToState(boss.chaseState);
            }
        }
    }

    public void ExitState(BossStateManager boss)
    {
        if (boss.smashIndicator != null)
        {
            boss.smashIndicator.SetActive(false);
        }

        boss.navMeshAgent.isStopped = false;

        boss.smashCooldownTimer = data.SmashCooldown;
    }

    private void PerformSmash(BossStateManager boss)
    {
        // 프레임에 따라 장판이 완전히 커지도록 보정
        if (fillRect != null) fillRect.localScale = Vector3.one;

        // 타격 시점에 맞춰 장판 즉시 끄기
        if (boss.smashIndicator != null)
        {
            boss.smashIndicator.SetActive(false);
        }

        if (data != null)
        {
            // OverlapSphere: 반경 내의 모든 충돌체 검사
            Collider[] hitColliders = Physics.OverlapSphere(boss.transform.position, data.SmashRadius);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    IDamageable target = hitCollider.GetComponent<IDamageable>();
                    if (target != null) target.TakeDamage(data.SmashDamage);

                    Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddExplosionForce(data.KnockbackForce * 100f, boss.transform.position, data.SmashRadius, 10.0f);
                    }
                }
            }
        }

        Debug.Log("쾅!!! (대지 분쇄)");
    }
}