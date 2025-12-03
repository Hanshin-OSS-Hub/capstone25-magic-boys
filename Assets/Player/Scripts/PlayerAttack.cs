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

    // 애니/락
    Animator animator;
    int attackHash;
    public float attackLockTime = 1.1f;
    float attackTimer;
    public bool IsAttacking { get; private set; }

    [Header("Melee (LMB)")]
    public float meleeRange = 2.5f;
    public float meleeRadius = 0.8f;
    public int meleeDamage = 20;
    public float hitDelay = 0f; // 애니 타이밍용

    [Header("Skill #1 (Q)")]
    public int skillMpCost = 15;
    public int baseSkillDamage = 30;
    public SkillProjectile projectilePrefab;
    public float projectileSpeed = 22f;
    public float projectileMaxDistance = 40f;
    public float skill1Cooldown = 5f;
    float skill1Remain;
    public bool IsSkill1Ready => skill1Remain <= 0f;
    public float Skill1CooldownRatio => Mathf.Clamp01(skill1Remain / skill1Cooldown);

    [Header("SFX/VFX - Melee")]
    public string meleeSwingSfxName;
    public AudioClip meleeSwingSfxClip;
    public string meleeHitSfxName;
    public AudioClip meleeHitSfxClip;
    [Range(0, 1)] public float meleeSfxVolume = 1f;
    public Vector2 meleePitchRandom = new Vector2(0.97f, 1.03f);

    [Header("SFX/VFX - Skill1 Cast")]
    public string castSfxName;
    public AudioClip castSfxClip;
    [Range(0, 1)] public float castSfxVolume = 1f;
    public Vector2 castPitchRandom = new Vector2(0.98f, 1.02f);
    public GameObject castVFX;

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
            var temp = new GameObject("~FirePointAuto");
            temp.transform.SetParent(transform);
            temp.transform.localPosition = new Vector3(0f, 1.5f, 0.2f);
            firePoint = temp.transform;
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

        if (skill1Remain > 0f) skill1Remain -= Time.deltaTime;

        // LMB = 근접, Q = 스킬
        if (input.IsAttackPressed && attackTimer <= 0f) StartMelee();
        if (input.IsSkill1Pressed) TryCastSkill1();
    }

    void StartMelee()
    {
        if (animator) animator.SetTrigger(attackHash);
        attackTimer = attackLockTime;
        IsAttacking = true;

        // 스윙 사운드(선행)
        Play3D(meleeSwingSfxName, meleeSwingSfxClip, firePoint.position, meleeSfxVolume, meleePitchRandom);

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
            var enemy = hit.collider.GetComponentInParent<EnemySimple>() ?? hit.collider.GetComponent<EnemySimple>();
            if (enemy)
            {
                Play3D(meleeHitSfxName, meleeHitSfxClip, hit.point, meleeSfxVolume, meleePitchRandom);
                enemy.TakeDamage(meleeDamage, player);
            }
        }
    }

    // ===== Skill1 (Q) =====
    void TryCastSkill1()
    {
        if (!IsSkill1Ready) { Debug.Log($"스킬 쿨다운 {skill1Remain:F1}s"); return; }
        if (!projectilePrefab) { Debug.LogWarning("SkillProjectile 프리팹이 비었습니다."); return; }
        if (!player || !player.SpendMP(skillMpCost)) { Debug.Log("MP가 부족합니다!"); return; }

        CastSkill1();
        skill1Remain = skill1Cooldown;
    }

    void CastSkill1()
    {
        Vector3 aimPoint = GetAimPoint(projectileMaxDistance);
        Vector3 origin = firePoint.position;
        Vector3 dir = (aimPoint - origin).normalized;

        int finalDamage = player ? player.GetMagicDamage(baseSkillDamage) : baseSkillDamage;

        var proj = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));
        proj.Launch(finalDamage, player, enemyMask, projectileSpeed, projectileMaxDistance);

        // 시전 SFX/VFX
        float castPitch = Random.Range(castPitchRandom.x, castPitchRandom.y);
        Play3D(castSfxName, castSfxClip, origin, castSfxVolume, new Vector2(castPitch, castPitch));

        if (castVFX)
            ParticleManager.instance?.PlayParticle(ParticleManager.ParticleType.FireCast, origin, Quaternion.LookRotation(dir));
    }

    Vector3 GetAimPoint(float maxDistance)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, maxDistance, worldMask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return ray.origin + ray.direction * maxDistance;
    }

    // ---- SFX helper ----
    void Play3D(string name, AudioClip clip, Vector3 pos, float vol, Vector2 pitchRange)
    {
        float pitch = Random.Range(pitchRange.x, pitchRange.y);
        if (!string.IsNullOrEmpty(name)) SoundManager.Instance?.PlaySFX3D(name, pos, vol, pitch);
        else if (clip) SoundManager.Instance?.PlaySFX3D(clip, pos, vol, pitch);
    }
}