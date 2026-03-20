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
        slowable?.ApplySlow(slowMultiplier, slowDuration);

        var dmgable = other.GetComponentInParent<IDamageable>();
        var simple = (dmgable == null) ? (other.GetComponentInParent<EnemySimple>() ?? other.GetComponent<EnemySimple>()) : null;

        if (dmgable != null) dmgable.TakeDamage(tickDamage);
        else if (simple != null) simple.TakeDamage(tickDamage, owner);
    }

    void OnTriggerExit(Collider other)
    {
        if (nextTickTime.ContainsKey(other))
            nextTickTime.Remove(other);
    }
}