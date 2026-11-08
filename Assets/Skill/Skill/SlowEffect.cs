using UnityEngine;
using UnityEngine.AI;

// 워터 필드 슬로우: 이동 속도와 애니메이션을 느리게 하고, 몸이 파랗게 빛나며 물방울이 떨어진다.
// ISlowable을 구현한 적이 없어서 WaterFieldSkill이 필요할 때 자동으로 붙인다.
public class SlowEffect : MonoBehaviour, ISlowable
{
    public Color tint = new Color(0.35f, 0.65f, 1f);
    public Color glow = new Color(0.1f, 0.45f, 1f) * 1.6f;

    private NavMeshAgent agent;
    private Animator animator;
    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private ParticleSystem drips;

    private float multiplier = 1f;
    private float remaining;
    private float originalSpeed;   // AI가 설정한 원래 속도
    private float appliedSpeed;    // 슬로우를 적용한 속도
    private float originalAnimSpeed = 1f;
    private bool active;
    private bool initialized;

    void Init()
    {
        if (initialized) return;
        initialized = true;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();

        // 몸에서 떨어지는 물방울
        var s = new VfxKit.BurstSettings
        {
            colorA = new Color(0.4f, 0.75f, 1f, 0.9f), colorB = new Color(0.2f, 0.5f, 1f, 0.7f),
            speed = new Vector2(0.2f, 0.8f), size = new Vector2(0.08f, 0.16f), life = new Vector2(0.5f, 0.9f),
            gravity = 1.2f, radius = 0.5f, additive = true
        };
        Bounds b = GetBounds();
        drips = VfxKit.CreateSystem("SlowDrips", b.center + Vector3.up * b.extents.y * 0.5f, s, transform);
        if (drips != null)
        {
            var shape = drips.shape; shape.radius = Mathf.Max(0.3f, Mathf.Max(b.extents.x, b.extents.z) * 0.8f);
            var em = drips.emission; em.rateOverTime = 18f;
            var main = drips.main; main.loop = true;
        }
    }

    Bounds GetBounds()
    {
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(transform.position + Vector3.up, Vector3.one);
        Bounds b = rs[0].bounds;
        foreach (var r in rs) if (!(r is ParticleSystemRenderer)) b.Encapsulate(r.bounds);
        return b;
    }

    public void ApplySlow(float slowMultiplier, float duration)
    {
        Init();
        slowMultiplier = Mathf.Clamp(slowMultiplier, 0.05f, 1f);
        // 더 강한 슬로우가 우선, 시간은 연장
        multiplier = active ? Mathf.Min(multiplier, slowMultiplier) : slowMultiplier;
        remaining = Mathf.Max(remaining, duration);

        if (!active)
        {
            active = true;
            if (agent != null) originalSpeed = agent.speed;
            if (animator != null) originalAnimSpeed = animator.speed;
            if (drips != null) drips.Play();
        }
        Apply();
    }

    void Update()
    {
        if (!active) return;

        // AI가 상태 전환 중에 속도를 바꿨으면 그 값을 새 기준으로 삼음
        if (agent != null && !Mathf.Approximately(agent.speed, appliedSpeed))
        {
            originalSpeed = agent.speed;
            Apply();
        }

        remaining -= Time.deltaTime;
        if (remaining <= 0f) End();
    }

    void LateUpdate()
    {
        // 피격 시 머티리얼이 바뀌어도(히트 플래시) 계속 파랗게 유지
        if (active) SetTint(true);
    }

    void Apply()
    {
        if (agent != null)
        {
            appliedSpeed = originalSpeed * multiplier;
            agent.speed = appliedSpeed;
        }
        if (animator != null) animator.speed = originalAnimSpeed * Mathf.Lerp(1f, multiplier, 0.7f);
        SetTint(true);
    }

    void End()
    {
        active = false;
        if (agent != null && Mathf.Approximately(agent.speed, appliedSpeed)) agent.speed = originalSpeed;
        if (animator != null) animator.speed = originalAnimSpeed;
        multiplier = 1f;
        if (drips != null) drips.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        SetTint(false);
    }

    void SetTint(bool on)
    {
        if (renderers == null) return;
        float pulse = 0.75f + Mathf.Sin(Time.time * 6f) * 0.25f;
        foreach (var r in renderers)
        {
            if (r == null || r is ParticleSystemRenderer) continue;
            if (on)
            {
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", tint);
                block.SetColor("_Color", tint);
                // 발광 색을 파랗게 덮어써서 용암 거미처럼 원래 빛나는 몬스터도 확실히 파래짐
                block.SetColor("_EmissionColor", glow * pulse);
                r.SetPropertyBlock(block);
            }
            else
            {
                block.Clear();
                r.SetPropertyBlock(block);
            }
        }
    }

    void OnDisable()
    {
        if (active) End();
    }
}
