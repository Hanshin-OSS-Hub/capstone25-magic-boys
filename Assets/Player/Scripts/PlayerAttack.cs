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

    [Header("Breakables")]
    public LayerMask breakableMask;   // ✅ 상자 레이어(예: Breakable)

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

    bool isRolling;
    float rollBlockTimer;

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

    // ===== Skill 3 (R) =====
    [Header("Skill #3 (R)")]
    public int skill3MpCost = 25;
    public int baseSkill3Damage = 60;
    public SkillProjectile projectile3Prefab;
    public float projectile3Speed = 28f;
    public float projectile3MaxDistance = 50f;
    public float skill3Cooldown = 9f;
    float skill3Remain;
    public bool IsSkill3Ready => skill3Remain <= 0f;
    public float Skill3CooldownRatio => (skill3Cooldown <= 0f) ? 0f : Mathf.Clamp01(skill3Remain / skill3Cooldown);

    [Header("Cast FX (R)")]
    public string cast3SfxName = "skill3_cast";
    [Range(0, 1)] public float cast3SfxVolume = 1f;
    public Vector2 cast3PitchRandom = new Vector2(0.98f, 1.02f);
    public ParticleType castParticle3 = ParticleType.FireCast;

    // ===== Skill 4 (T) =====
    [Header("Skill #4 (T)")]
    public int skill4MpCost = 30;
    public int baseSkill4Damage = 75;
    public SkillProjectile projectile4Prefab;
    public float projectile4Speed = 30f;
    public float projectile4MaxDistance = 55f;
    public float skill4Cooldown = 11f;
    float skill4Remain;
    public bool IsSkill4Ready => skill4Remain <= 0f;
    public float Skill4CooldownRatio => (skill4Cooldown <= 0f) ? 0f : Mathf.Clamp01(skill4Remain / skill4Cooldown);

    [Header("Cast FX (T)")]
    public string cast4SfxName = "skill4_cast";
    [Range(0, 1)] public float cast4SfxVolume = 1f;
    public Vector2 cast4PitchRandom = new Vector2(0.98f, 1.02f);
    public ParticleType castParticle4 = ParticleType.SparkCast;

    // ===== Skill 5 (Y) =====
    [Header("Skill #5 (Y)")]
    public int skill5MpCost = 35;
    public int baseSkill5Damage = 90;
    public SkillProjectile projectile5Prefab;
    public float projectile5Speed = 32f;
    public float projectile5MaxDistance = 60f;
    public float skill5Cooldown = 13f;
    float skill5Remain;
    public bool IsSkill5Ready => skill5Remain <= 0f;
    public float Skill5CooldownRatio => (skill5Cooldown <= 0f) ? 0f : Mathf.Clamp01(skill5Remain / skill5Cooldown);

    [Header("Cast FX (Y)")]
    public string cast5SfxName = "skill5_cast";
    [Range(0, 1)] public float cast5SfxVolume = 1f;
    public Vector2 cast5PitchRandom = new Vector2(0.98f, 1.02f);
    public ParticleType castParticle5 = ParticleType.FireCast;

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

        // 공격 락 타이머
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f) IsAttacking = false;
        }

        // 쿨다운 감소
        TickCooldowns();

        // 롤 상태 감지 & 공격 차단
        UpdateRollGuard();

        // 평타 (롤 중/직후 차단)
        if (!isRolling && rollBlockTimer <= 0f && input.IsAttackPressed && attackTimer <= 0f)
            StartMelee();

        // 스킬 입력
        if (input.IsSkill1Pressed) TryCastSkill1();
        if (input.IsSkill2Pressed) TryCastSkill2();
        if (input.IsSkill3Pressed) TryCastSkill3();
        if (input.IsSkill4Pressed) TryCastSkill4();
        if (input.IsSkill5Pressed) TryCastSkill5();
    }

    void TickCooldowns()
    {
        if (skill1Remain > 0f) skill1Remain = Mathf.Max(0f, skill1Remain - Time.deltaTime);
        if (skill2Remain > 0f) skill2Remain = Mathf.Max(0f, skill2Remain - Time.deltaTime);
        if (skill3Remain > 0f) skill3Remain = Mathf.Max(0f, skill3Remain - Time.deltaTime);
        if (skill4Remain > 0f) skill4Remain = Mathf.Max(0f, skill4Remain - Time.deltaTime);
        if (skill5Remain > 0f) skill5Remain = Mathf.Max(0f, skill5Remain - Time.deltaTime);
    }

    bool IsSkillUnlocked(int slotIndex0Based)
    {
        // 매니저 없으면(세팅 전) 잠금 체크 생략
        if (SkillProgressionManager.Instance == null) return true;
        return SkillProgressionManager.Instance.IsUnlocked(slotIndex0Based);
    }

    void UpdateRollGuard()
    {
        if (!animator) return;

        bool nowRoll = animator.GetCurrentAnimatorStateInfo(0).IsName(rollStateName);

        if (nowRoll && !isRolling)
        {
            isRolling = true;
            animator.ResetTrigger(attackHash);
            IsAttacking = false;
        }
        else if (!nowRoll && isRolling)
        {
            isRolling = false;
            rollBlockTimer = postRollBlock;
            animator.ResetTrigger(attackHash);
        }

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

        int mask = enemyMask | breakableMask;

        if (Physics.SphereCast(origin, meleeRadius, dir, out var hit, meleeRange, mask, QueryTriggerInteraction.Collide))
        {
            // 1) 적 우선
            var dmgable = hit.collider.GetComponentInParent<IDamageable>();
            if (dmgable != null) { dmgable.TakeDamage(meleeDamage); return; }

            var enemy = hit.collider.GetComponentInParent<EnemySimple>() ?? hit.collider.GetComponent<EnemySimple>();
            if (enemy) { enemy.TakeDamage(meleeDamage, player); return; }

            // 2) 상자(브레이커블)
            var chest = hit.collider.GetComponentInParent<BreakableChest>() ?? hit.collider.GetComponent<BreakableChest>();
            if (chest) { chest.TakeDamage(meleeDamage); return; }
        }
    }

    // ===== Skill1 (Q) =====
    void TryCastSkill1()
    {
        // Skill1(슬롯0)은 기본 해금으로 가정
        if (!IsSkill1Ready) return;
        if (!projectilePrefab) { Debug.LogWarning("Skill1 프리팹 비었음"); return; }
        if (!player || !player.SpendMP(skillMpCost)) { Debug.Log("MP 부족"); return; }

        CastProjectile(projectilePrefab, baseSkillDamage, projectileSpeed, projectileMaxDistance,
            castSfxName, castSfxVolume, castPitchRandom, castParticle1);

        skill1Remain = skill1Cooldown;
    }

    // ===== Skill2 (E) =====
    void TryCastSkill2()
    {
        if (!IsSkillUnlocked(1)) { Debug.Log("스킬2 잠김"); return; }
        if (!IsSkill2Ready) return;
        if (!projectile2Prefab) { Debug.LogWarning("Skill2 프리팹 비었음"); return; }
        if (!player || !player.SpendMP(skill2MpCost)) { Debug.Log("MP 부족"); return; }

        CastProjectile(projectile2Prefab, baseSkill2Damage, projectile2Speed, projectile2MaxDistance,
            cast2SfxName, cast2SfxVolume, cast2PitchRandom, castParticle2);

        skill2Remain = skill2Cooldown;
    }

    // ===== Skill3 (R) =====
    void TryCastSkill3()
    {
        if (!IsSkillUnlocked(2)) { Debug.Log("스킬3 잠김"); return; }
        if (!IsSkill3Ready) return;
        if (!projectile3Prefab) { Debug.LogWarning("Skill3 프리팹 비었음"); return; }
        if (!player || !player.SpendMP(skill3MpCost)) { Debug.Log("MP 부족"); return; }

        CastProjectile(projectile3Prefab, baseSkill3Damage, projectile3Speed, projectile3MaxDistance,
            cast3SfxName, cast3SfxVolume, cast3PitchRandom, castParticle3);

        skill3Remain = skill3Cooldown;
    }

    // ===== Skill4 (T) =====
    void TryCastSkill4()
    {
        if (!IsSkillUnlocked(3)) { Debug.Log("스킬4 잠김"); return; }
        if (!IsSkill4Ready) return;
        if (!projectile4Prefab) { Debug.LogWarning("Skill4 프리팹 비었음"); return; }
        if (!player || !player.SpendMP(skill4MpCost)) { Debug.Log("MP 부족"); return; }

        CastProjectile(projectile4Prefab, baseSkill4Damage, projectile4Speed, projectile4MaxDistance,
            cast4SfxName, cast4SfxVolume, cast4PitchRandom, castParticle4);

        skill4Remain = skill4Cooldown;
    }

    // ===== Skill5 (Y) =====
    void TryCastSkill5()
    {
        if (!IsSkillUnlocked(4)) { Debug.Log("스킬5 잠김"); return; }
        if (!IsSkill5Ready) return;
        if (!projectile5Prefab) { Debug.LogWarning("Skill5 프리팹 비었음"); return; }
        if (!player || !player.SpendMP(skill5MpCost)) { Debug.Log("MP 부족"); return; }

        CastProjectile(projectile5Prefab, baseSkill5Damage, projectile5Speed, projectile5MaxDistance,
            cast5SfxName, cast5SfxVolume, cast5PitchRandom, castParticle5);

        skill5Remain = skill5Cooldown;
    }

    // ===== 공용 캐스트 =====
    void CastProjectile(SkillProjectile prefab, int baseDamage, float speed, float maxDistance,
                       string sfxName, float sfxVolume, Vector2 pitchRange, ParticleType castParticle)
    {
        Vector3 aimPoint = GetAimPoint(maxDistance);
        Vector3 origin = firePoint.position;
        Vector3 dir = (aimPoint - origin).normalized;

        int dmg = player ? player.GetMagicDamage(baseDamage) : baseDamage;

        var proj = Instantiate(prefab, origin, Quaternion.LookRotation(dir));
        proj.Launch(dmg, player, enemyMask, speed, maxDistance);

        float pitch = Random.Range(pitchRange.x, pitchRange.y);
        if (!string.IsNullOrEmpty(sfxName))
            SoundManager.Instance?.PlaySFX2D(sfxName, sfxVolume, pitch);

        ParticleManager.Instance?.Play(castParticle, origin, Quaternion.LookRotation(dir));
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