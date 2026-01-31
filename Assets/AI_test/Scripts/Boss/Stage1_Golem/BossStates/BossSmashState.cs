using UnityEngine;
using UnityEngine.UI;

public class BossSmashState : IBossState
{
    private float timer;
    private float castTime;
    private bool hasSmashed;

    private RectTransform fillRect; // 장판 크기 조절용
    private GolemData data;         

    public void EnterState(BossStateManager boss)
    {
        boss.navMeshAgent.isStopped = true;
        hasSmashed = false;
        if (boss.animator != null) boss.animator.SetTrigger("doSmash");

        // 데이터 가져오기 (형변환 후 저장)
        data = boss.stats as GolemData;

        // 데이터가 있으면 그 값을 쓰고, 없으면 기본값 1.5초 
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

        // 장판이 중심에서 밖으로 커지는 연출
        if (fillRect != null && castTime > 0)
        {
            float progress = Mathf.Clamp01((castTime - timer) / castTime);
            fillRect.localScale = Vector3.one * progress;
        }

        // 시간이 다 됐고, 아직 공격 안 했으면 실행
        if (timer <= 0 && !hasSmashed)
        {
            PerformSmash(boss);
            hasSmashed = true;
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

        if (data != null)
        {
            // OverlapSphere: 반경 내의 모든 충돌체 검사
            Collider[] hitColliders = Physics.OverlapSphere(boss.transform.position, data.SmashRadius);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    IDamageable target = hitCollider.GetComponent<IDamageable>();
                    if (target != null)
                    {
                        target.TakeDamage(data.SmashDamage);
                    }

                    Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddExplosionForce(data.KnockbackForce * 100f, boss.transform.position, data.SmashRadius, 10.0f);
                    }
                }
            }
        }

        Debug.Log("쾅!!! (대지 분쇄)");

        boss.TransitionToState(boss.chaseState);
    }
}