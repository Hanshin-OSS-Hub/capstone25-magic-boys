using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class WaterFieldSkill : MonoBehaviour
{
    PlayerStats owner;
    LayerMask enemyMask;
    int tickDamage;
    float lifeTime;
    float tickInterval;
    float slowMultiplier;
    float slowDuration;

    readonly Dictionary<Collider, float> nextTickTime = new Dictionary<Collider, float>();

    public void Init(PlayerStats owner, LayerMask enemyMask, int tickDamage, float lifeTime, float tickInterval, float slowMultiplier, float slowDuration)
    {
        this.owner = owner;
        this.enemyMask = enemyMask;
        this.tickDamage = tickDamage;
        this.lifeTime = lifeTime;
        this.tickInterval = Mathf.Max(0.1f, tickInterval);
        this.slowMultiplier = Mathf.Clamp(slowMultiplier, 0.1f, 1f);
        this.slowDuration = Mathf.Max(0.05f, slowDuration);

        SphereCollider sc = GetComponent<SphereCollider>();
        sc.isTrigger = true;

        // 트리거 이벤트는 둘 중 하나에 Rigidbody가 있어야 발생한다.
        // 프리팹·적 모두 Rigidbody가 없어서 OnTriggerStay가 아예 호출되지 않았음 → 키네마틱 Rigidbody 보장
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyMask) == 0) return;

        float nextTime;
        if (!nextTickTime.TryGetValue(other, out nextTime))
            nextTime = 0f;

        if (Time.time < nextTime) return;

        nextTickTime[other] = Time.time + tickInterval;

        var slowable = other.GetComponentInParent<ISlowable>();
        if (slowable == null)
        {
            // No enemy implements ISlowable yet: attach SlowEffect to the enemy root
            var agent = other.GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) slowable = agent.gameObject.AddComponent<SlowEffect>();
        }
        slowable?.ApplySlow(slowMultiplier, slowDuration);

        var dmgable = other.GetComponentInParent<IDamageable>();
        var simple = (dmgable == null) ? (other.GetComponentInParent<EnemySimple>() ?? other.GetComponent<EnemySimple>()) : null;

        if (dmgable != null) dmgable.TakeDamage(tickDamage);
        else if (simple != null) simple.TakeDamage(tickDamage, owner);
        if (dmgable != null || simple != null)
            HitFeedback.Play(other, other.bounds.center, tickDamage, HitElement.Water, other.transform.position - transform.position, 1.5f);
    }

    void OnTriggerExit(Collider other)
    {
        if (nextTickTime.ContainsKey(other))
            nextTickTime.Remove(other);
    }
}