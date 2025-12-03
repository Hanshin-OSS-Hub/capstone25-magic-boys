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

    [Header("Hit SFX")]
    public string hitSfxName = "fireball_hit";
    [Range(0, 1)] public float hitSfxVolume = 1f;
    public Vector2 hitPitchRandom = new Vector2(0.95f, 1.05f);

    public void Launch(int damage, PlayerStats owner, LayerMask enemyMask, float speed, float maxDistance)
    {
        this.damage = damage;
        this.owner = owner;
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

        var enemy = other.GetComponentInParent<EnemySimple>() ?? other.GetComponent<EnemySimple>();
        if (!enemy) return;

        // 트리거에서 내부로 파고들어 숨는 문제 방지용 보정
        Vector3 hitPos = transform.position - transform.forward * 0.08f;
        Quaternion hitRot = Quaternion.LookRotation(-transform.forward);

        float pitch = Random.Range(hitPitchRandom.x, hitPitchRandom.y);
        SoundManager.Instance?.PlaySFX3D(hitSfxName, hitPos, hitSfxVolume, pitch);

        ParticleManager.Instance?.Play(ParticleType.FireHit, hitPos, hitRot);

        enemy.TakeDamage(damage, owner);
        Destroy(gameObject);
    }
}