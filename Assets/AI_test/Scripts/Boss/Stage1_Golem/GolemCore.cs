using UnityEngine;

public class GolemCore : MonoBehaviour, IDamageable
{
    // 인스펙터에서 보스 본체를 연결해줘야 함
    public BossStateManager bossBody;

    public void TakeDamage(float damage)
    {
        // 데미지 양과 상관없이, 맞기만 하면 보스 기믹 발동
        bossBody.OnCoreHit();
    }
}