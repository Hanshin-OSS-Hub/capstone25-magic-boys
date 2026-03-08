using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class SkillProjectile : MonoBehaviour
{
    int damage;
    PlayerStats owner;
    LayerMask enemyMask;
    float speed;
    float remainDistance;
    bool launched;

    Rigidbody rb;
    Collider col;

    public void Launch(int damage, PlayerStats owner, LayerMask enemyMask, float speed, float maxDistance)
    {
        this.damage = damage;
        this.owner = owner;                 // 지금은 미사용(필요시 히트로그/가시성)
        this.enemyMask = enemyMask;
        this.speed = speed;
        this.remainDistance = maxDistance;

        if (!rb) rb = GetComponent<Rigidbody>();
        if (!col) col = GetComponent<Collider>();

        rb.isKinematic = true;
        rb.useGravity = false;
        col.isTrigger = true;

        launched = true;
    }

    void Update()
    {
        if (!launched) return;

        float move = speed * Time.deltaTime;
        transform.position += transform.forward * move;
        remainDistance -= move;

        if (remainDistance <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!launched) return;
        if (((1 << other.gameObject.layer) & enemyMask) == 0) return;

        // ▼ EnemySimple → IDamageable
        var dmg = other.GetComponentInParent<IDamageable>() ?? other.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}