using System.Collections;
using UnityEngine;

public class ThunderRainSkill : MonoBehaviour
{
    public GameObject strikeVfx;   // 여기엔 SummonStorm 넣기
    public GameObject warningVfx;  // 안 쓰면 None

    PlayerStats owner;
    LayerMask enemyMask;
    int damagePerStrike;
    float areaRadius;
    float singleStrikeRadius;
    int strikeCount;
    float warningDuration;
    float totalStrikeDuration;

    public void Init(PlayerStats owner, LayerMask enemyMask, int damagePerStrike, float areaRadius, float singleStrikeRadius, int strikeCount, float warningDuration, float totalStrikeDuration)
    {
        this.owner = owner;
        this.enemyMask = enemyMask;
        this.damagePerStrike = damagePerStrike;
        this.areaRadius = areaRadius;
        this.singleStrikeRadius = singleStrikeRadius;
        this.strikeCount = Mathf.Max(1, strikeCount);
        this.warningDuration = Mathf.Max(0f, warningDuration);
        this.totalStrikeDuration = Mathf.Max(0.1f, totalStrikeDuration);

        StartCoroutine(CoThunderRain());
    }

    IEnumerator CoThunderRain()
    {
        if (warningVfx)
            Instantiate(warningVfx, transform.position, Quaternion.identity);

        if (warningDuration > 0f)
            yield return new WaitForSeconds(warningDuration);

        // 완성형 폭풍 프리팹은 한 번만 생성
        if (strikeVfx)
            Instantiate(strikeVfx, transform.position, Quaternion.identity);

        float interval = totalStrikeDuration / strikeCount;

        for (int i = 0; i < strikeCount; i++)
        {
            DealDamageOnce();
            yield return new WaitForSeconds(interval);
        }

        Destroy(gameObject);
    }

    void DealDamageOnce()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, areaRadius, enemyMask, QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            var dmgable = hit.GetComponentInParent<IDamageable>();
            var simple = (dmgable == null) ? (hit.GetComponentInParent<EnemySimple>() ?? hit.GetComponent<EnemySimple>()) : null;

            if (dmgable != null) dmgable.TakeDamage(damagePerStrike);
            else if (simple != null) simple.TakeDamage(damagePerStrike, owner);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, areaRadius);
    }
}