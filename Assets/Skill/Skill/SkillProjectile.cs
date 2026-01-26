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

    [Header("Hit SFX/VFX")]
    public string hitSfxName;
    public AudioClip hitSfxClip;
    [Range(0, 1)] public float hitSfxVolume = 1f;
    public Vector2 hitPitchRandom = new Vector2(0.95f, 1.05f);
    public ParticleType hitParticle = ParticleType.FireHit;   // 프리팹별로 선택
    public GameObject hitVfxOverride; // (선택) 개별 프리팹에서 직접 프리팹 사용하고 싶으면 지정

    public void Launch(int damage, PlayerStats owner, LayerMask enemyMask, float speed, float maxDistance)
    {
        this.damage = damage;
        this.owner = owner;
        this.enemyMask = enemyMask;
        this.speed = speed;
        this.remainDistance = maxDistance;

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

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

        // 타격 대상 찾기 (IDamageable 우선, 없으면 EnemySimple)
        var dmgable = other.GetComponentInParent<IDamageable>();
        var simple = (dmgable == null) ? (other.GetComponentInParent<EnemySimple>() ?? other.GetComponent<EnemySimple>()) : null;
        if (dmgable == null && simple == null) return;

        Vector3 hitPos = other.ClosestPoint(transform.position);
        Quaternion hitRot = Quaternion.LookRotation(-transform.forward);

        // 사운드
        float pitch = Random.Range(hitPitchRandom.x, hitPitchRandom.y);
        if (!string.IsNullOrEmpty(hitSfxName))
            SoundManager.Instance?.PlaySFX3D(hitSfxName, hitPos, hitSfxVolume, pitch);
        else if (hitSfxClip)
            SoundManager.Instance?.PlaySFX3D(hitSfxClip, hitPos, hitSfxVolume, pitch);

        // 파티클
        if (hitVfxOverride)
        {
            var go = Instantiate(hitVfxOverride, hitPos, hitRot);
            var ps = go.GetComponentInChildren<ParticleSystem>();
            if (ps) Destroy(go, ps.main.duration + ps.main.startLifetimeMultiplier + 0.25f);
            else Destroy(go, 2f);
        }
        else
        {
            ParticleManager.Instance?.Play(hitParticle, hitPos, hitRot);
        }

        // 데미지 적용
        if (dmgable != null) dmgable.TakeDamage(damage);
        else simple.TakeDamage(damage, owner);

        Destroy(gameObject);
    }
}