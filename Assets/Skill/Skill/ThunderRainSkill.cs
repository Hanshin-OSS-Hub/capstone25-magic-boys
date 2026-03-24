using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThunderRainSkill : MonoBehaviour
{
    public GameObject strikeVfx;
    public GameObject warningVfx;

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
            warningVfx.SetActive(true);

        if (warningDuration > 0f)
            yield return new WaitForSeconds(warningDuration);

        if (warningVfx)
            warningVfx.SetActive(false);

        float interval = totalStrikeDuration / strikeCount;

        for (int i = 0; i < strikeCount; i++)
        {
            StrikeOnce();
            yield return new WaitForSeconds(interval);
        }

        Destroy(gameObject);
    }

    void StrikeOnce()
    {
        Vector2 circle = Random.insideUnitCircle * areaRadius;
        Vector3 strikePos = transform.position + new Vector3(circle.x, 0f, circle.y);

        if (strikeVfx)
            Instantiate(strikeVfx, strikePos, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(strikePos, singleStrikeRadius, enemyMask, QueryTriggerInteraction.Collide);
        HashSet<Transform> damagedRoots = new HashSet<Transform>();

        foreach (var hit in hits)
        {
            Transform root = hit.transform.root;
            if (!damagedRoots.Add(root)) continue;

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