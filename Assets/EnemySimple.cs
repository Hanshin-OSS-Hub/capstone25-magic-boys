using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EnemySimple : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 40;
    public int contactDamage = 10;   // 플레이어가 닿았을 때 주는 데미지
    public int expReward = 20;

    int currentHP;

    void Awake()
    {
        currentHP = maxHP;

        // 간단 충돌 설정 (트리거 + 키네마틱)
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerStats>();
        if (player != null)
        {
            player.TakeDamage(contactDamage);
        }
    }

    public void TakeDamage(int amount, PlayerStats killer = null)
    {
        currentHP -= amount;
        if (currentHP <= 0) Die(killer);
    }

    void Die(PlayerStats killer)
    {
        if (killer != null) killer.AddExp(expReward);
        Destroy(gameObject);
    }
}