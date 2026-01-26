using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerAttack : MonoBehaviour
{
    // Refs
    public PlayerInput input;
    public PlayerStats player;
    public Camera cam;
    public Transform firePoint;

    [Header("Layers")]
    public LayerMask enemyMask;
    public LayerMask worldMask;

    // Animator / lock
    Animator animator;
    int attackHash;
    public float attackLockTime = 1.1f;
    float attackTimer;
    public bool IsAttacking { get; private set; }

    // ===== Roll guard =====
    [Header("Roll Guard")]
    [Tooltip("애니메이터의 '롤' 상태 이름 (정확히 일치해야 함)")]
    public string rollStateName = "Roll";
    [Tooltip("롤 종료 후 잠깐 평타를 막는 시간")]
    public float postRollBlock = 0.15f;

    bool isRolling;           // 현재 롤 중인지
    float rollBlockTimer;     // 롤 종료 후 블록 타이머

    // ===== Melee (LMB) =====
    [Header("Melee (LMB)")]
    public float meleeRange = 2.5f;
    public float meleeRadius = 0.8f;
    public int meleeDamage = 20;
    public float hitDelay = 0f;

    // ===== Skill 1 (Q) =====
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
    [Header("Cast FX (Q)")]
    public string castSfxName = "fireball_cast";
    [Range(0, 1)] public float castSfxVolume = 1f;
    public Vector2 castPitchRandom = new Vector2(0.98f, 1.02f);
    public ParticleType castParticle1 = ParticleType.FireCast;

    // ===== Skill 2 (E) =====
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
    [Header("Cast FX (E)")]
    public string cast2SfxName = "spark_cast";
    [Range(0, 1)] public float cast2SfxVolume = 1f;
    public Vector2 cast2PitchRandom = new Vector2(0.98f, 1.02f);
    public ParticleType castParticle2 = ParticleType.SparkCast;

    [Header("Debug")]
    public bool drawDebug = true;
    public Color debugMeleeColor = Color.yellow;

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

        // 공격 락 타이머/애니 락
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f) IsAttacking = false;
        }

        // 쿨다운
        if (skill1Remain > 0f) skill1Remain = Mathf.Max(0f, skill1Remain - Time.deltaTime);
        if (skill2Remain > 0f) skill2Remain = Mathf.Max(0f, skill2Remain - Time.deltaTime);

        // ===== 롤 상태 감지 & 공격 차단 =====
        UpdateRollGuard();

        // 입력 처리 (롤 중/직후에는 평타 금지)
        if (!isRolling && rollBlockTimer <= 0f && input.IsAttackPressed && attackTimer <= 0f)
            StartMelee();

        if (input.IsSkill1Pressed) TryCastSkill1();
        if (input.IsSkill2Pressed) TryCastSkill2();
    }

    void UpdateRollGuard()
    {
        if (!animator) return;

        bool nowRoll = animator.GetCurrentAnimatorStateInfo(0).IsName(rollStateName);

        // 롤 시작 감지
        if (nowRoll && !isRolling)
        {
            isRolling = true;
            // 혹시 남아있을 Attack 트리거/상태를 정리
            animator.ResetTrigger(attackHash);
            IsAttacking = false;
        }
        // 롤 종료 감지
        else if (!nowRoll && isRolling)
        {
            isRolling = false;
            rollBlockTimer = postRollBlock;
            // 안전하게 한 번 더 트리거 정리
            animator.ResetTrigger(attackHash);
        }

        // 롤 종료 후 블록 타이머 감소
        if (rollBlockTimer > 0f) rollBlockTimer -= Time.deltaTime;
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

        if (drawDebug) Debug.DrawRay(origin, dir * meleeRange, debugMeleeColor, 0.25f);

        if (Physics.SphereCast(origin, meleeRadius, dir, out var hit, meleeRange, enemyMask, QueryTriggerInteraction.Collide))
        {
            // IDamageable 우선, 없으면 EnemySimple
            var dmgable = hit.collider.GetComponentInParent<IDamageable>();
            if (dmgable != null) { dmgable.TakeDamage(meleeDamage); return; }

            var enemy = hit.collider.GetComponentInParent<EnemySimple>() ?? hit.collider.GetComponent<EnemySimple>();
            if (enemy) enemy.TakeDamage(meleeDamage, player);
        }
    }

    // ===== Skill1 (Q) =====
    void TryCastSkill1()
    {
        if (!IsSkill1Ready) { Debug.Log($"스킬1 쿨 {skill1Remain:F1}s"); return; }
        if (!projectilePrefab) { Debug.LogWarning("Skill1 프리팹 비었음"); return; }
        if (!player || !player.SpendMP(skillMpCost)) { Debug.Log("MP 부족"); return; }

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

        float pitch = Random.Range(castPitchRandom.x, castPitchRandom.y);
        SoundManager.Instance?.PlaySFX2D(castSfxName, castSfxVolume, pitch);
        ParticleManager.Instance?.Play(castParticle1, origin, Quaternion.LookRotation(dir));
    }

    // ===== Skill2 (E) =====
    void TryCastSkill2()
    {
        if (!IsSkill2Ready) { Debug.Log($"스킬2 쿨 {skill2Remain:F1}s"); return; }
        if (!projectile2Prefab) { Debug.LogWarning("Skill2 프리팹 비었음"); return; }
        if (!player || !player.SpendMP(skill2MpCost)) { Debug.Log("MP 부족"); return; }

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

        float pitch2 = Random.Range(cast2PitchRandom.x, cast2PitchRandom.y);
        SoundManager.Instance?.PlaySFX2D(cast2SfxName, cast2SfxVolume, pitch2);
        ParticleManager.Instance?.Play(castParticle2, origin, Quaternion.LookRotation(dir));
    }

    // ===== 공용 =====
    Vector3 GetAimPoint(float maxDistance)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, maxDistance, worldMask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return ray.origin + ray.direction * maxDistance;
    }
}