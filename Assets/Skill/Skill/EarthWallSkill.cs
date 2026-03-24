using System.Collections;
using UnityEngine;

public class EarthWallSkill : MonoBehaviour
{
    [Header("References")]
    public BoxCollider damageBox;
    public Collider solidCollider;

    [Header("Spawn Damage")]
    public bool dealSpawnDamage = true;

    [Header("Contact Damage")]
    public bool dealContactDamage = true;
    public float contactTickInterval = 0.5f;
    [Range(0f, 1f)] public float contactDamageRatio = 0.35f;

    [Header("Safe Spawn")]
    public float solidEnableDelay = 0.05f;
    public float liftPlayerTopOffset = 0.05f;

    [Header("Debug")]
    public bool debugLog = false;

    private PlayerStats owner;
    private LayerMask enemyMask;
    private int spawnDamage;
    private int contactDamage;
    private Transform casterRoot;

    public void Init(PlayerStats owner, LayerMask enemyMask, int damage, float lifeTime, Transform casterRoot)
    {
        this.owner = owner;
        this.enemyMask = enemyMask;
        this.spawnDamage = damage;
        this.contactDamage = Mathf.Max(1, Mathf.RoundToInt(damage * contactDamageRatio));
        this.casterRoot = casterRoot;

        if (!solidCollider)
            solidCollider = GetComponent<Collider>();

        if (!damageBox)
            FindDamageBox();

        if (solidCollider)
            solidCollider.enabled = false;

        if (dealSpawnDamage && damageBox)
            DealSpawnDamageOnce();

        EarthWallDamageBox damageRelay = null;
        if (damageBox)
            damageRelay = damageBox.GetComponent<EarthWallDamageBox>();

        if (damageRelay)
            damageRelay.Setup(this, owner, enemyMask, contactDamage, contactTickInterval, dealContactDamage);

        StartCoroutine(FinishSpawnSafe());

        Destroy(gameObject, lifeTime);

        if (debugLog)
            Debug.Log($"[EarthWallSkill] Init / spawnDamage={spawnDamage}, contactDamage={contactDamage}");
    }

    IEnumerator FinishSpawnSafe()
    {
        yield return null;

        if (casterRoot != null && solidCollider != null)
        {
            TryLiftCasterToTop();
        }

        yield return new WaitForSeconds(solidEnableDelay);

        if (solidCollider != null)
            solidCollider.enabled = true;
    }

    void TryLiftCasterToTop()
    {
        CharacterController cc = casterRoot.GetComponent<CharacterController>();
        Collider casterCol = casterRoot.GetComponent<Collider>();

        Bounds wallBounds = solidCollider.bounds;
        Bounds casterBounds;

        if (cc != null)
            casterBounds = cc.bounds;
        else if (casterCol != null)
            casterBounds = casterCol.bounds;
        else
            return;

        if (!wallBounds.Intersects(casterBounds))
            return;

        Vector3 pos = casterRoot.position;
        float casterHalfHeight = casterBounds.extents.y;
        float targetY = wallBounds.max.y + casterHalfHeight + liftPlayerTopOffset;

        if (cc != null)
        {
            cc.enabled = false;
            casterRoot.position = new Vector3(pos.x, targetY, pos.z);
            cc.enabled = true;
        }
        else
        {
            casterRoot.position = new Vector3(pos.x, targetY, pos.z);
        }

        if (debugLog)
            Debug.Log("[EarthWallSkill] 플레이어를 벽 위로 이동시켜 겹침 해소");
    }

    void FindDamageBox()
    {
        BoxCollider[] all = GetComponentsInChildren<BoxCollider>(true);

        foreach (var box in all)
        {
            if (box == null) continue;
            if (box.name.ToLower().Contains("damage"))
            {
                damageBox = box;
                return;
            }
        }
    }

    void DealSpawnDamageOnce()
    {
        Vector3 center = damageBox.transform.TransformPoint(damageBox.center);
        Vector3 halfExtents = Vector3.Scale(damageBox.size * 0.5f, AbsVec(damageBox.transform.lossyScale));
        Quaternion rotation = damageBox.transform.rotation;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, enemyMask, QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            if (!hit) continue;
            ApplyDamageToTarget(hit, spawnDamage);
        }
    }

    public void ApplyDamageToTarget(Collider hit, int damage)
    {
        if (!hit) return;

        var dmgable = hit.GetComponentInParent<IDamageable>();
        var simple = (dmgable == null)
            ? (hit.GetComponentInParent<EnemySimple>() ?? hit.GetComponent<EnemySimple>())
            : null;

        if (dmgable != null)
        {
            dmgable.TakeDamage(damage);

            if (debugLog)
                Debug.Log($"[EarthWallSkill] IDamageable hit: {hit.transform.root.name}, damage={damage}");
        }
        else if (simple != null)
        {
            simple.TakeDamage(damage, owner);

            if (debugLog)
                Debug.Log($"[EarthWallSkill] EnemySimple hit: {hit.transform.root.name}, damage={damage}");
        }
    }

    Vector3 AbsVec(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    void OnDrawGizmosSelected()
    {
        if (!damageBox) return;

        Gizmos.color = Color.green;

        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            damageBox.transform.TransformPoint(damageBox.center),
            damageBox.transform.rotation,
            Vector3.Scale(damageBox.size, AbsVec(damageBox.transform.lossyScale))
        );

        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = old;
    }
}