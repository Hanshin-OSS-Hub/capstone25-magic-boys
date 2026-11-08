using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Y 궁극기 - 대장간 운석: 조준 지점에 불타는 운석이 떨어져 넓은 범위에 큰 피해 + 밀쳐내기.
// 연출은 SineVFX MeteorStrike(운석이 1초 동안 떨어진 뒤 폭발)를 사용한다.
public class MeteorStrikeSkill : MonoBehaviour
{
    private PlayerStats owner;
    private LayerMask enemyMask;
    private int damage;
    private float radius;
    private float impactDelay;
    private float knockback;

    [Tooltip("폭발 뒤 불타는 땅 지속 시간(초), 1초마다 화상 피해")]
    public float burnDuration = 3f;
    [Tooltip("화상 1회 피해 = 폭발 피해 x 이 비율")]
    public float burnDamageRatio = 0.15f;

    public void Init(PlayerStats owner, LayerMask enemyMask, int damage, float radius,
        float impactDelay, float knockback, GameObject vfxPrefab, Vector3 casterPosition)
    {
        this.owner = owner;
        this.enemyMask = enemyMask;
        this.damage = damage;
        this.radius = radius;
        this.impactDelay = impactDelay;
        this.knockback = knockback;

        if (vfxPrefab == null && FxLibrary.Instance != null) vfxPrefab = FxLibrary.Instance.meteorStrike;
        if (vfxPrefab != null)
        {
            // 운석이 시전자 쪽 하늘에서 날아오도록 방향을 맞춤 (이펙트의 운석은 -Z 쪽 위에서 출발)
            Vector3 dir = transform.position - casterPosition;
            dir.y = 0f;
            Quaternion rot = dir.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dir.normalized) : Quaternion.identity;
            var vfx = Instantiate(vfxPrefab, transform.position, rot, transform);
            foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true)) ps.Play(true);
        }

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        // 떨어지는 동안 하늘이 붉게 물들고 땅이 울림
        float t = 0f;
        while (t < impactDelay)
        {
            VfxKit.Shake(transform.position, Mathf.Lerp(0.01f, 0.06f, t / impactDelay), 0.2f, 30f);
            t += 0.15f;
            yield return new WaitForSeconds(0.15f);
        }
        Impact();
        StartCoroutine(Aftermath());
        Destroy(gameObject, 8f);
    }

    // 폭발 뒤: 주변에서 용암 기둥이 연달아 솟구치고, 불타는 땅이 잠시 남아 지속 피해
    private IEnumerator Aftermath()
    {
        var lib = FxLibrary.Instance;
        if (lib != null && lib.lavaPool != null)
            VfxKit.Spawn(lib.lavaPool, transform.position + Vector3.up * 0.05f, Quaternion.identity, burnDuration + 1.5f);

        int pillars = 5;
        float startAngle = Random.Range(0f, 360f);
        for (int i = 0; i < pillars; i++)
        {
            float a = (startAngle + i * 360f / pillars) * Mathf.Deg2Rad;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * Random.Range(0.55f, 0.8f);
            if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out RaycastHit g, 8f, ~LayerMask.GetMask("Enemy", "Boss", "Player"), QueryTriggerInteraction.Ignore))
                p = g.point;
            if (lib != null && lib.lavaPillar != null)
                VfxKit.Spawn(lib.lavaPillar, p, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), 4f);
            VfxKit.Burst(p, VfxKit.Embers(25, 0.6f), 2.5f);
            if (lib != null && i % 2 == 0) VfxKit.PlayClip(lib.lavaBurst, p, 0.6f);
            VfxKit.Shake(p, 0.12f, 0.2f, 25f);
            yield return new WaitForSeconds(0.12f);
        }

        // 불타는 땅: 1초마다 화상 피해
        int ticks = Mathf.Max(1, Mathf.RoundToInt(burnDuration));
        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(1f);
            DamageArea(Mathf.Max(1, Mathf.RoundToInt(damage * burnDamageRatio)), 0f, false);
        }
    }

    private void Impact()
    {
        var lib = FxLibrary.Instance;
        if (lib != null) VfxKit.PlayClip(lib.meteorImpact, transform.position, 1f);

        // 순간 슬로모션 + 화면 섬광 + 폭발 빛 + 큰 흔들림 + 충격파
        VfxKit.SlowMotion(0.25f, 0.3f);
        VfxKit.ScreenFlash(new Color(1f, 0.75f, 0.45f), 0.55f);
        VfxKit.LightFlash(transform.position + Vector3.up * 2f, new Color(1f, 0.5f, 0.15f), 14f, radius * 3f, 1.0f);
        VfxKit.Shake(transform.position, 0.45f, 0.7f, 40f);
        VfxKit.Shockwave(transform.position, radius * 1.3f, new Color(0.5f, 0.38f, 0.3f, 0.8f), 110);
        VfxKit.Burst(transform.position, VfxKit.Embers(120, radius * 0.3f), 3.5f);

        DamageArea(damage, knockback, true);
    }

    private void DamageArea(int baseDamage, float push, bool falloffByDistance)
    {
        var hits = Physics.OverlapSphere(transform.position, radius, enemyMask, QueryTriggerInteraction.Collide);
        var done = new HashSet<Object>();
        foreach (var hit in hits)
        {
            var dmgable = hit.GetComponentInParent<IDamageable>();
            var simple = dmgable == null ? hit.GetComponentInParent<EnemySimple>() : null;
            Object key = dmgable as Object ?? simple;
            if (key == null || !done.Add(key)) continue;
            if (dmgable is PlayerStats) continue;

            // 중심에 가까울수록 더 아프고 더 멀리 날아감
            float d = Vector3.Distance(hit.transform.position, transform.position);
            float falloff = falloffByDistance ? Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(d / radius)) : 1f;
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * falloff));

            if (dmgable != null) dmgable.TakeDamage(finalDamage);
            else simple.TakeDamage(finalDamage, owner);

            HitFeedback.Play(hit, hit.bounds.center, finalDamage, HitElement.Fire,
                hit.transform.position - transform.position, push * falloff);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
