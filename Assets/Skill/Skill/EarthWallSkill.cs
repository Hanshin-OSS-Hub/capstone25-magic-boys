using System.Collections.Generic;
using UnityEngine;

public class EarthWallSkill : MonoBehaviour
{
    public BoxCollider damageBox;
    public float contactTickInterval = 0.5f;
    [Range(0f, 1f)] public float contactDamageRatio = 0.35f;

    PlayerStats owner;
    LayerMask enemyMask;
    int spawnDamage;
    int contactDamage;
    float nextContactTime;

    public void Init(PlayerStats owner, LayerMask enemyMask, int spawnDamage, float lifeTime)
    {
        this.owner = owner;
        this.enemyMask = enemyMask;
        this.spawnDamage = spawnDamage;
        this.contactDamage = Mathf.Max(1, Mathf.RoundToInt(spawnDamage * contactDamageRatio));

        if (!damageBox)
            damageBox = GetComponentInChildren<BoxCollider>();

        DealAreaDamage(spawnDamage);
        nextContactTime = Time.time + contactTickInterval;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (damageBox == null) return;

        if (Time.time >= nextContactTime)
        {
            nextContactTime = Time.time + contactTickInterval;
            DealAreaDamage(contactDamage);
        }
    }

    void DealAreaDamage(int damage)
    {
        if (damageBox == null) return;

        Vector3 center = damageBox.transform.TransformPoint(damageBox.center);
        Vector3 halfExtents = Vector3.Scale(damageBox.size * 0.5f, AbsVec(damageBox.transform.lossyScale));
        Quaternion rot = damageBox.transform.rotation;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rot, enemyMask, QueryTriggerInteraction.Collide);
        HashSet<Transform> damagedRoots = new HashSet<Transform>();

        foreach (var hit in hits)
        {
            Transform root = hit.transform.root;
            if (!damagedRoots.Add(root)) continue;

            var dmgable = hit.GetComponentInParent<IDamageable>();
            var simple = (dmgable == null) ? (hit.GetComponentInParent<EnemySimple>() ?? hit.GetComponent<EnemySimple>()) : null;

            if (dmgable != null) dmgable.TakeDamage(damage);
            else if (simple != null) simple.TakeDamage(damage, owner);
        }
    }

    Vector3 AbsVec(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }
}