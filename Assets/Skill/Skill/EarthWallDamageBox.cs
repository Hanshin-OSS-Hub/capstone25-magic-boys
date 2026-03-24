using System.Collections.Generic;
using UnityEngine;

public class EarthWallDamageBox : MonoBehaviour
{
    private EarthWallSkill ownerSkill;
    private PlayerStats owner;
    private LayerMask enemyMask;
    private int damage;
    private float tickInterval;
    private bool enabledDamage;

    private readonly Dictionary<Transform, float> nextHitTime = new Dictionary<Transform, float>();

    public void Setup(EarthWallSkill ownerSkill, PlayerStats owner, LayerMask enemyMask, int damage, float tickInterval, bool enabledDamage)
    {
        this.ownerSkill = ownerSkill;
        this.owner = owner;
        this.enemyMask = enemyMask;
        this.damage = damage;
        this.tickInterval = tickInterval;
        this.enabledDamage = enabledDamage;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box)
            box.isTrigger = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (!enabledDamage) return;
        if (((1 << other.gameObject.layer) & enemyMask) == 0) return;

        Transform root = other.transform.root;

        if (nextHitTime.TryGetValue(root, out float nextTime))
        {
            if (Time.time < nextTime) return;
        }

        nextHitTime[root] = Time.time + tickInterval;

        if (ownerSkill != null)
            ownerSkill.ApplyDamageToTarget(other, damage);
    }

    void OnTriggerExit(Collider other)
    {
        Transform root = other.transform.root;
        if (nextHitTime.ContainsKey(root))
            nextHitTime.Remove(root);
    }
}