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
        this.owner = owner;
        this.enemyMask = enemyMask;
        this.speed = speed;
        this.remainDistance = maxDistance;

        if (rb == null) rb = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();

        // 물리 세팅(투사체용)
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

        if (remainDistance <= 0f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!launched) return;

        // Enemy 레이어만 맞추고 싶으면 레이어 비교
        if (((1 << other.gameObject.layer) & enemyMask) == 0)
            return;

        var enemy = other.GetComponentInParent<EnemySimple>() ?? other.GetComponent<EnemySimple>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage, owner); // 사망 시 Enemy가 EXP 지급
            Destroy(gameObject);
        }
    }
}