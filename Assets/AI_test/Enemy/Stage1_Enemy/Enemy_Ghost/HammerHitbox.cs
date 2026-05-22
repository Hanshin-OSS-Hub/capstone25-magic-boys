using System.Collections.Generic;
using UnityEngine;

public class HammerHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 10f;

    private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    private void OnEnable()
    {
        hitTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyStateManager enemy = other.GetComponentInParent<EnemyStateManager>();
        if (enemy == null)
            return;

        GameObject rootObject = enemy.gameObject;

        if (hitTargets.Contains(rootObject))
            return;

        hitTargets.Add(rootObject);
        enemy.TakeDamage(damage);
    }
}