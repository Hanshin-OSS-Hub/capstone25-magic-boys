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

    [Header("SFX/VFX - Hit")]
    public string hitSfxName;
    public AudioClip hitSfxClip;
    [Range(0, 1)] public float hitSfxVolume = 1f;
    public Vector2 hitPitchRandom = new Vector2(0.95f, 1.05f);
    public GameObject hitVFX;

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

        Vector3 hitPos = other.ClosestPoint(transform.position);
        Quaternion hitRot = Quaternion.LookRotation(-transform.forward);

        float pitch = Random.Range(hitPitchRandom.x, hitPitchRandom.y);
        if (!string.IsNullOrEmpty(hitSfxName))
            SoundManager.Instance?.PlaySFX3D(hitSfxName, hitPos, hitSfxVolume, pitch);
        else if (hitSfxClip)
            SoundManager.Instance?.PlaySFX3D(hitSfxClip, hitPos, hitSfxVolume, pitch);

        if (hitVFX)
            ParticleManager.instance?.PlayParticle(ParticleManager.ParticleType.FireHit, hitPos, hitRot);

        enemy.TakeDamage(damage, owner);
        Destroy(gameObject);
    }
}