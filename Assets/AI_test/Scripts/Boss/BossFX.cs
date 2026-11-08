using System.Collections;
using UnityEngine;

// 보스(용암 골렘) 패턴 연출. 각 BossState에서 한 줄씩 호출한다.
public static class BossFX
{
    static readonly string[] MeteorParts = { "Meteor_01", "MeteorFire", "Indicator" };

    /// <summary>보스 기준점(몸 중간)이 아니라 실제 바닥 높이</summary>
    public static Vector3 Ground(Vector3 from)
    {
        int mask = ~LayerMask.GetMask("Boss", "Player", "Enemy", "Ignore Raycast", "UI");
        if (Physics.Raycast(from + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 30f, mask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return from;
    }

    // 보스가 3.4배 크기라 코드 파티클(불씨/증기/먼지)이 원래 크기로는 거의 안 보임 → 보스 크기에 맞춰 키움
    static float BossK(Transform boss) => boss == null ? 1f : Mathf.Clamp(boss.lossyScale.x / 2f, 1f, 3f);

    static VfxKit.BurstSettings Big(VfxKit.BurstSettings s, float k)
    {
        s.size *= k;
        s.radius *= k;
        s.speed *= Mathf.Sqrt(k);
        return s;
    }

    // SineVFX 이펙트는 반경 약 5m 기준으로 만들어져 있음 (Ground 지름 10)
    const float SineVfxBaseRadius = 5f;
    static float FitRadius(float radius) => Mathf.Clamp(radius / SineVfxBaseRadius, 0.5f, 3f);

    static void SpawnScaled(GameObject prefab, Vector3 pos, float life, float scale, float timeOffset = 0f, string[] hide = null)
    {
        var go = VfxKit.Spawn(prefab, pos, Quaternion.identity, life, timeOffset, hide);
        if (go != null) go.transform.localScale = Vector3.one * scale; // 파티클 scalingMode가 Hierarchy라 같이 커짐
    }

    /// <summary>보스 공격 범위 장판을 바닥에 붙임</summary>
    public static void PlaceOnGround(Transform indicator, Transform boss)
    {
        if (indicator == null || boss == null) return;
        Vector3 g = Ground(boss.position);
        indicator.position = new Vector3(boss.position.x, g.y + 0.05f, boss.position.z);
    }

    // ───────── 대지 분쇄(Smash) ─────────
    /// <summary>기 모으기: 발밑에 용암이 끓어오르고 땅이 울림</summary>
    public static void SmashCharge(Transform boss, float castTime, Transform indicator = null, float radius = 5f)
    {
        PlaceOnGround(indicator, boss);
        var lib = FxLibrary.Instance;
        if (lib != null && lib.lavaPool != null)
            SpawnScaled(lib.lavaPool, Ground(boss.position) + Vector3.up * 0.05f, castTime + 1.5f, FitRadius(radius) * 0.6f);
        VfxRunner.Instance.StartCoroutine(ChargeRumble(boss, castTime));
    }

    static IEnumerator ChargeRumble(Transform boss, float castTime)
    {
        float t = 0f;
        while (t < castTime && boss != null)
        {
            float k = t / castTime;
            VfxKit.Shake(boss.position, Mathf.Lerp(0.01f, 0.05f, k), 0.25f, 30f);
            var e = Big(VfxKit.Embers(Mathf.RoundToInt(Mathf.Lerp(10, 35, k)), 2f), BossK(boss));
            e.speed = new Vector2(2f, 5f) * Mathf.Sqrt(BossK(boss));
            VfxKit.Burst(Ground(boss.position), e, 2f);
            yield return new WaitForSeconds(0.25f);
            t += 0.25f;
        }
    }

    /// <summary>내려찍는 순간: 용암 폭발 + 충격파 + 큰 흔들림 + 소리</summary>
    public static void SmashImpact(Vector3 pos, float radius)
    {
        pos = Ground(pos);
        var lib = FxLibrary.Instance;
        if (lib != null)
        {
            // 운석 이펙트에서 운석만 빼고 폭발 부분을 바로 재생
            SpawnScaled(lib.meteorStrike, pos, 6f, FitRadius(radius), 1f, MeteorParts);
            VfxKit.PlayClip(lib.bossSmashImpact, pos, 1f);
        }
        VfxKit.Shockwave(pos, radius, new Color(0.45f, 0.36f, 0.3f, 0.75f), 90);
        VfxKit.Burst(pos, Big(VfxKit.Embers(140, radius * 0.4f), 1.8f), 3f);
        VfxKit.LightFlash(pos + Vector3.up * 1.5f, new Color(1f, 0.45f, 0.15f), 10f, radius * 2.5f, 0.6f);
        VfxKit.Shake(pos, 0.4f, 0.55f, 35f);
    }

    // ───────── 돌진(Rush) ─────────
    public static void RushStart(Transform boss)
    {
        if (boss == null) return;
        float bk = BossK(boss);
        VfxKit.Burst(Ground(boss.position), Big(VfxKit.Dust(40, 1.2f), bk), 3f);
        var lib = FxLibrary.Instance;
        if (lib != null) VfxKit.PlayClip(lib.bossRush, boss.position, 0.8f);

        // 달리는 동안 발밑 먼지 + 불씨 자국
        if (boss.Find("RushTrail") == null)
        {
            var s = Big(VfxKit.Dust(0, 1f), bk);
            s.life = new Vector2(0.6f, 1.1f);
            var ps = VfxKit.CreateSystem("RushTrail", boss.position, s, boss);
            if (ps != null)
            {
                ps.transform.position = Ground(boss.position) + Vector3.up * 0.1f;
                var em = ps.emission; em.rateOverDistance = 3f; // 월드 거리 기준 (이전엔 보스 스케일로 나눠서 1m당 1개꼴로 거의 안 보였음)
                var main = ps.main; main.loop = true;
                ps.Play();
            }
            var e = Big(VfxKit.Embers(0, 0.8f), bk);
            var ember = VfxKit.CreateSystem("RushEmbers", Ground(boss.position) + Vector3.up * 0.1f, e, boss);
            if (ember != null)
            {
                var em2 = ember.emission; em2.rateOverDistance = 4f;
                var main2 = ember.main; main2.loop = true;
                ember.Play();
            }
        }
    }

    public static void RushEnd(Transform boss)
    {
        if (boss == null) return;
        foreach (var n in new[] { "RushTrail", "RushEmbers" })
        {
            var t = boss.Find(n);
            if (t == null) continue;
            var ps = t.GetComponent<ParticleSystem>();
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            t.SetParent(null, true);
            Object.Destroy(t.gameObject, 2f);
        }
    }

    public static void RushHit(Vector3 pos)
    {
        VfxKit.Burst(Ground(pos), Big(VfxKit.Dust(40, 0.8f), 1.5f), 3f);
        VfxKit.Shake(pos, 0.3f, 0.35f, 25f);
    }

    // ───────── 바위 던지기(Throw) ─────────
    public static void ThrowRock(Transform rock, Vector3 handPos)
    {
        VfxKit.Burst(handPos, VfxKit.Embers(25, 0.5f), 2f);
        if (rock == null) return;
        // 날아가는 바위 뒤로 불꽃 꼬리
        var s = VfxKit.Embers(0, 0.3f);
        s.speed = new Vector2(0.2f, 1f);
        s.size = new Vector2(0.25f, 0.6f);
        s.life = new Vector2(0.3f, 0.6f);
        s.gravity = -0.2f;
        s.stretch = false;
        var ps = VfxKit.CreateSystem("RockFireTrail", rock.position, s, rock);
        if (ps != null)
        {
            ps.transform.localPosition = Vector3.zero;
            var em = ps.emission; em.rateOverTime = 60f;
            var main = ps.main; main.loop = true;
            ps.Play();
        }
        var dust = VfxKit.Dust(0, 0.3f);
        dust.size = new Vector2(0.4f, 0.8f);
        var ps2 = VfxKit.CreateSystem("RockSmokeTrail", rock.position, dust, rock);
        if (ps2 != null)
        {
            ps2.transform.localPosition = Vector3.zero;
            var em2 = ps2.emission; em2.rateOverTime = 25f;
            var main2 = ps2.main; main2.loop = true;
            ps2.Play();
        }
    }

    // ───────── 과열(Overload) / 배출(Venting) ─────────
    public static void Overload(Transform boss, float duration)
    {
        if (boss == null) return;
        VfxRunner.Instance.StartCoroutine(OverloadRoutine(boss, duration));
    }

    static IEnumerator OverloadRoutine(Transform boss, float duration)
    {
        var lib = FxLibrary.Instance;
        float bk = BossK(boss);
        if (lib != null && lib.lavaPool != null)
            SpawnScaled(lib.lavaPool, Ground(boss.position) + Vector3.up * 0.05f, duration + 1.5f, bk);

        var lightGo = new GameObject("OverloadGlow");
        lightGo.transform.SetParent(boss, false);
        lightGo.transform.localPosition = Vector3.up * 1.2f;
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.3f, 0.05f); l.range = 9f * bk; l.shadows = LightShadows.None;

        float t = 0f;
        while (t < duration && boss != null)
        {
            l.intensity = 4f + Mathf.Sin(t * 12f) * 2f;
            var e = Big(VfxKit.Embers(16, 1.5f), bk);
            VfxKit.Burst(boss.position + Vector3.up * 1.5f, e, 2f);
            yield return new WaitForSeconds(0.15f);
            t += 0.15f;
        }
        if (lightGo != null) Object.Destroy(lightGo);
    }

    public static void Venting(Transform boss, float duration)
    {
        if (boss == null) return;
        VfxRunner.Instance.StartCoroutine(VentingRoutine(boss, duration));
    }

    static IEnumerator VentingRoutine(Transform boss, float duration)
    {
        float bk = BossK(boss);
        float t = 0f;
        while (t < duration && boss != null)
        {
            // 등과 어깨에서 증기가 뿜어져 나옴 (약점 노출 상태)
            Vector3 basePos = boss.position + Vector3.up * (0.6f * boss.lossyScale.y / 2.5f); // 기준점이 몸 중간이라 조금만 위
            for (int i = 0; i < 3; i++)
            {
                Vector3 off = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * 0.8f * bk;
                VfxKit.Burst(basePos + off, Big(VfxKit.Steam(18, 0.3f), bk), 2.5f);
            }
            yield return new WaitForSeconds(0.35f);
            t += 0.35f;
        }
    }
}
