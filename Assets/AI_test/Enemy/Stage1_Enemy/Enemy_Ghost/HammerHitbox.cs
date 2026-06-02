using System.Collections.Generic;
using UnityEngine;

public class HammerHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float hitCooldown = 0.5f;

    private Dictionary<GameObject, float> lastHitTimes = new Dictionary<GameObject, float>();
    private PlayerAttack playerAttack;

    private void Awake()
    {
        // PlayerAttack 컴포넌트는 부모 또는 루트에 있을 것으로 예상됨
        playerAttack = GetComponentInParent<PlayerAttack>();
    }

    private void OnEnable()
    {
        lastHitTimes.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessHit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        ProcessHit(other);
    }

    private void ProcessHit(Collider other)
    {
        // 공격 중이 아닐 때는 무시
        if (playerAttack != null && !playerAttack.IsAttacking)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        GameObject rootObject = (damageable as MonoBehaviour)?.gameObject;
        if (rootObject == null)
            return;

        // 플레이어에게는 데미지를 입히지 않음
        if (rootObject.CompareTag("Player"))
            return;

        // 쿨타임 체크
        if (lastHitTimes.TryGetValue(rootObject, out float lastHitTime))
        {
            if (Time.time - lastHitTime < hitCooldown)
                return;
        }

        // 데미지 적용 및 쿨타임 갱신
        lastHitTimes[rootObject] = Time.time;
        damageable.TakeDamage(damage);
        
        Debug.Log($"[HammerHitbox] {rootObject.name}에게 {damage} 데미지 적용 (다음 타격 가능까지 {hitCooldown}초)");
    }
}