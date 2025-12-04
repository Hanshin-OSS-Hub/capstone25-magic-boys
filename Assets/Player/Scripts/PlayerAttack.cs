using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerAttack : MonoBehaviour
{
    public PlayerInput input;
    public PlayerStats player;
    public Camera cam;
    public Transform firePoint;

    [Header("Layers")]
    public LayerMask enemyMask;
    public LayerMask worldMask;

    Animator animator;
    int attackHash;
    public float attackLockTime = 1.1f;
    float attackTimer;
    public bool IsAttacking { get; private set; }

    [Header("Melee (LMB)")]
    public float meleeRange = 2.5f;
    public float meleeRadius = 0.8f;
    public int meleeDamage = 20;
    public float hitDelay = 0f;

    [Header("Skill #1 (Q)")]
    public int skillMpCost = 15;
    public int baseSkillDamage = 30;
    public SkillProjectile projectilePrefab;
    public float projectileSpeed = 22f;
    public float projectileMaxDistance = 40f;
    public float skill1Cooldown = 5f;
    float skill1Remain;
    public bool IsSkill1Ready => skill1Remain <= 0f;
    public float Skill1CooldownRatio => (skill1Cooldown <= 0f) ? 0f : Mathf.Clamp01(skill1Remain / skill1Cooldown);

    [Header("Skill #2 (E)")]
    public int skill2MpCost = 20;
    public int baseSkill2Damage = 45;
    public SkillProjectile projectile2Prefab;
    public float projectile2Speed = 26f;
    public float projectile2MaxDistance = 45f;
    public float skill2Cooldown = 7f;
    float skill2Remain;
    public bool IsSkill2Ready => skill2Remain <= 0f;
    public float Skill2CooldownRatio => (skill2Cooldown <= 0f) ? 0f : Mathf.Clamp01(skill2Remain / skill2Cooldown);

    void Start()
    {
        if (!input) input = GetComponent<PlayerInput>();
        if (!player) player = GetComponent<PlayerStats>();
        if (!cam) cam = Camera.main;

        if (!firePoint)
        {
            var go = new GameObject("~FirePointAuto");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 1.5f, 0.2f);
            firePoint = go.transform;
        }

        animator = GetComponentInChildren<Animator>();
        if (animator) attackHash = Animator.StringToHash("Attack");
    }

    void Update()
    {
        if (StatsPanelToggle.UIBlocked) return;
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
        if (!input) return;

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f) IsAttacking = false;
        }

        if (skill1Remain > 0f) skill1Remain = Mathf.Max(0f, skill1Remain - Time.deltaTime);
        if (skill2Remain > 0f) skill2Remain = Mathf.Max(0f, skill2Remain - Time.deltaTime);

        if (input.IsAttackPressed && attackTimer <= 0f) StartMelee();
        if (input.IsSkill1Pressed) TryCastSkill1();
        if (input.IsSkill2Pressed) TryCastSkill2();
    }

    // ===== Melee =====
    void StartMelee()
    {
        if (animator) animator.SetTrigger(attackHash);
        attackTimer = attackLockTime;
        IsAttacking = true;

        if (hitDelay <= 0f) DoMelee();
        else Invoke(nameof(DoMelee), hitDelay);
    }

    void DoMelee()
    {
        Vector3 origin = firePoint ? firePoint.position : transform.position + Vector3.up * 1.2f;
        Vector3 dir = transform.forward;

        if (Physics.SphereCast(origin, meleeRadius, dir, out var hit, meleeRange, enemyMask, QueryTriggerInteraction.Collide))
        {
            var target = hit.collider.GetComponentInParent<IDamageable>() ?? hit.collider.GetComponent<IDamageable>();
            if (target != null) target.TakeDamage(meleeDamage);
        }
    }

    // ===== Skill1 (Q) =====
    void TryCastSkill1()
    {
        if (!IsSkill1Ready) return;
        if (!projectilePrefab) { Debug.LogWarning("Skill1 prefab missing"); return; }
        if (!player || !player.SpendMP(skillMpCost)) { Debug.Log("Not enough MP"); return; }

        CastSkill1();
        skill1Remain = skill1Cooldown;
    }

    void CastSkill1()
    {
        Vector3 aimPoint = GetAimPoint(projectileMaxDistance);
        Vector3 origin = firePoint.position;
        Vector3 dir = (aimPoint - origin).normalized;

        int dmg = player ? player.GetMagicDamage(baseSkillDamage) : baseSkillDamage;

        var proj = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));
        proj.Launch(dmg, player, enemyMask, projectileSpeed, projectileMaxDistance);
    }

    // ===== Skill2 (E) =====
    void TryCastSkill2()
    {
        if (!IsSkill2Ready) return;
        if (!projectile2Prefab) { Debug.LogWarning("Skill2 prefab missing"); return; }
        if (!player || !player.SpendMP(skill2MpCost)) { Debug.Log("Not enough MP"); return; }

        CastSkill2();
        skill2Remain = skill2Cooldown;
    }

    void CastSkill2()
    {
        Vector3 aimPoint = GetAimPoint(projectile2MaxDistance);
        Vector3 origin = firePoint.position;
        Vector3 dir = (aimPoint - origin).normalized;

        int dmg = player ? player.GetMagicDamage(baseSkill2Damage) : baseSkill2Damage;

        var proj = Instantiate(projectile2Prefab, origin, Quaternion.LookRotation(dir));
        proj.Launch(dmg, player, enemyMask, projectile2Speed, projectile2MaxDistance);
    }

    Vector3 GetAimPoint(float maxDistance)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, maxDistance, worldMask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return ray.origin + ray.direction * maxDistance;
    }
}