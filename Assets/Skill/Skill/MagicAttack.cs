using UnityEngine;

public class MagicAttack : MonoBehaviour
{
    [Header("Refs")]
    public PlayerInput playerInput;
    public PlayerStats playerStats;
    public Camera aimCamera;
    public LayerMask groundMask = -1;
    public LayerMask enemyMask = -1;

    [Header("Common")]
    public float maxAimDistance = 25f;
    public float spawnForwardOffset = 0.8f;
    public float spawnHeightOffset = 1.2f;

    [Header("Q - Fireball")]
    public Transform qSpawnPoint;
    public GameObject qProjectilePrefab;
    public int qMpCost = 5;
    public float qCooldown = 1.2f;
    public int qBaseDamage = 8;
    public float qProjectileSpeed = 14f;
    public float qMaxDistance = 20f;

    [Header("E - Water Field")]
    public GameObject eWaterFieldPrefab;
    public int eMpCost = 12;
    public float eCooldown = 6f;
    public int eBaseTickDamage = 4;
    public float eDuration = 4f;
    public float eTickInterval = 0.5f;
    [Range(0.1f, 1f)] public float eSlowMultiplier = 0.6f;
    public float eSlowDuration = 0.7f;

    [Header("R - Earth Wall")]
    public GameObject rEarthWallPrefab;
    public int rMpCost = 15;
    public float rCooldown = 8f;
    public int rBaseDamage = 10;
    public float rLifeTime = 5f;
    public float rSpawnDistance = 2.5f;

    [Header("T - Thunder Rain")]
    public GameObject tThunderRainPrefab;
    public int tMpCost = 18;
    public float tCooldown = 10f;
    public int tBaseDamagePerStrike = 6;
    public float tAreaRadius = 4f;
    public float tSingleStrikeRadius = 1.3f;
    public int tStrikeCount = 8;
    public float tWarningDuration = 0.7f;
    public float tTotalStrikeDuration = 2.4f;

    [Header("Y - Reserved")]
    public int yMpCost = 20;
    public float yCooldown = 12f;

    float qRemain;
    float eRemain;
    float rRemain;
    float tRemain;
    float yRemain;

    void Awake()
    {
        if (!playerInput) playerInput = GetComponent<PlayerInput>();
        if (!playerStats) playerStats = GetComponent<PlayerStats>();
        if (!aimCamera) aimCamera = Camera.main;
    }

    void Update()
    {
        TickCooldowns();

        if (StatsPanelToggle.UIBlocked) return;
        if (!playerInput || !playerStats) return;

        if (playerInput.IsSkillQPressed) TryCastQ();
        if (playerInput.IsSkillEPressed) TryCastE();
        if (playerInput.IsSkillRPressed) TryCastR();
        if (playerInput.IsSkillTPressed) TryCastT();
        if (playerInput.IsSkillYPressed) TryCastY();
    }

    void TickCooldowns()
    {
        if (qRemain > 0f) qRemain -= Time.deltaTime;
        if (eRemain > 0f) eRemain -= Time.deltaTime;
        if (rRemain > 0f) rRemain -= Time.deltaTime;
        if (tRemain > 0f) tRemain -= Time.deltaTime;
        if (yRemain > 0f) yRemain -= Time.deltaTime;
    }

    bool IsUnlocked(int slotIndex)
    {
        if (SkillProgressionManager.Instance == null) return true;
        return SkillProgressionManager.Instance.IsUnlocked(slotIndex);
    }

    bool TrySpendMP(int amount, string skillName)
    {
        if (playerStats.SpendMP(amount)) return true;
        Debug.Log($"{skillName} MP 부족");
        return false;
    }

    Quaternion GetFlatLookRotation()
    {
        Vector3 forward = aimCamera ? aimCamera.transform.forward : transform.forward;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up);

        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    Vector3 GetDefaultSpawnPosition()
    {
        Quaternion rot = GetFlatLookRotation();
        return transform.position + Vector3.up * spawnHeightOffset + (rot * Vector3.forward * spawnForwardOffset);
    }

    bool TryGetAimPoint(out Vector3 point)
    {
        Ray ray = aimCamera
            ? new Ray(aimCamera.transform.position, aimCamera.transform.forward)
            : new Ray(transform.position + Vector3.up, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }

        point = ray.origin + ray.direction * maxAimDistance;
        return false;
    }

    void TryCastQ()
    {
        if (!IsUnlocked(0)) return;
        if (qRemain > 0f) return;
        if (!qProjectilePrefab) return;
        if (!TrySpendMP(qMpCost, "Q")) return;

        Quaternion rot = GetFlatLookRotation();
        Vector3 pos = qSpawnPoint ? qSpawnPoint.position : GetDefaultSpawnPosition();

        GameObject go = Instantiate(qProjectilePrefab, pos, rot);
        SkillProjectile projectile = go.GetComponent<SkillProjectile>();

        if (!projectile)
        {
            Debug.LogWarning("Q 프리팹에 SkillProjectile이 없음");
            Destroy(go);
            return;
        }

        int damage = playerStats.GetMagicDamage(qBaseDamage);
        projectile.Launch(damage, playerStats, enemyMask, qProjectileSpeed, qMaxDistance);

        qRemain = qCooldown;
    }

    void TryCastE()
    {
        if (!IsUnlocked(1)) return;
        if (eRemain > 0f) return;
        if (!eWaterFieldPrefab) return;
        if (!TrySpendMP(eMpCost, "E")) return;

        TryGetAimPoint(out Vector3 point);

        GameObject go = Instantiate(eWaterFieldPrefab, point + Vector3.up * 0.05f, Quaternion.identity);
        WaterFieldSkill water = go.GetComponent<WaterFieldSkill>();

        if (!water)
        {
            Debug.LogWarning("E 프리팹에 WaterFieldSkill이 없음");
            Destroy(go);
            return;
        }

        int tickDamage = playerStats.GetMagicDamage(eBaseTickDamage);
        water.Init(playerStats, enemyMask, tickDamage, eDuration, eTickInterval, eSlowMultiplier, eSlowDuration);

        eRemain = eCooldown;
    }

    void TryCastR()
    {
        if (!IsUnlocked(2)) return;
        if (rRemain > 0f) return;
        if (!rEarthWallPrefab) return;
        if (!TrySpendMP(rMpCost, "R")) return;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = transform.forward;

        Vector3 spawnPos = transform.position + forward * rSpawnDistance;
        Ray downRay = new Ray(spawnPos + Vector3.up * 5f, Vector3.down);
        if (Physics.Raycast(downRay, out RaycastHit groundHit, 10f, groundMask, QueryTriggerInteraction.Ignore))
            spawnPos = groundHit.point;

        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        GameObject go = Instantiate(rEarthWallPrefab, spawnPos, rot);
        EarthWallSkill wall = go.GetComponent<EarthWallSkill>();

        if (!wall)
        {
            Debug.LogWarning("R 프리팹에 EarthWallSkill이 없음");
            Destroy(go);
            return;
        }

        int damage = playerStats.GetMagicDamage(rBaseDamage);
        wall.Init(playerStats, enemyMask, damage, rLifeTime);

        rRemain = rCooldown;
    }

    void TryCastT()
    {
        if (!IsUnlocked(3)) return;
        if (tRemain > 0f) return;
        if (!tThunderRainPrefab) return;
        if (!TrySpendMP(tMpCost, "T")) return;

        TryGetAimPoint(out Vector3 point);

        GameObject go = Instantiate(tThunderRainPrefab, point + Vector3.up * 0.05f, Quaternion.identity);
        ThunderRainSkill thunder = go.GetComponent<ThunderRainSkill>();

        if (!thunder)
        {
            Debug.LogWarning("T 프리팹에 ThunderRainSkill이 없음");
            Destroy(go);
            return;
        }

        int damage = playerStats.GetMagicDamage(tBaseDamagePerStrike);
        thunder.Init(playerStats, enemyMask, damage, tAreaRadius, tSingleStrikeRadius, tStrikeCount, tWarningDuration, tTotalStrikeDuration);

        tRemain = tCooldown;
    }

    void TryCastY()
    {
        if (!IsUnlocked(4)) return;
        if (yRemain > 0f) return;

        Debug.Log("Y 스킬은 아직 미정");
        yRemain = yCooldown;
    }

    public float GetCooldownRatio(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return qCooldown <= 0f ? 0f : Mathf.Clamp01(qRemain / qCooldown);
            case 1: return eCooldown <= 0f ? 0f : Mathf.Clamp01(eRemain / eCooldown);
            case 2: return rCooldown <= 0f ? 0f : Mathf.Clamp01(rRemain / rCooldown);
            case 3: return tCooldown <= 0f ? 0f : Mathf.Clamp01(tRemain / tCooldown);
            case 4: return yCooldown <= 0f ? 0f : Mathf.Clamp01(yRemain / yCooldown);
        }
        return 0f;
    }
}