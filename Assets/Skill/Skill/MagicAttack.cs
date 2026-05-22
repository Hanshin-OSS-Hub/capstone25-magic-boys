using UnityEngine;

public class MagicAttack : MonoBehaviour
{
    [Header("Refs")]
    public PlayerInput playerInput;
    public PlayerStats playerStats;
    public Camera aimCamera;
    public LayerMask groundMask = -1;
    public LayerMask enemyMask = -1;

    [Header("Skill Keys")]
    public KeyCode skill1Key = KeyCode.Q;
    public KeyCode skill2Key = KeyCode.E;
    public KeyCode skill3Key = KeyCode.R;
    public KeyCode skill4Key = KeyCode.T;
    public KeyCode skill5Key = KeyCode.Y;

    [Header("Cast Result Particle")]
    public Transform castEffectPoint;
    public ParticleType successParticle = ParticleType.CastSuccess;
    public ParticleType failParticle = ParticleType.CastFail;
    public float resultParticleLifetime = 2f;

    [Header("Cast Fail Sound")]
    public string failSfxName = "Cast_Fail";
    [Range(0f, 1f)] public float failSfxVolume = 1f;
    public float failSfxPitch = 1f;

    [Header("Cast Success Voice - Fireball")]
    public string fireballSuccessSfxName = "Fireball_Voice";
    [Range(0f, 1f)] public float fireballSuccessSfxVolume = 1f;
    public float fireballSuccessSfxPitch = 1f;

    [Header("Cast Success Voice - Water")]
    public string waterSuccessSfxName = "Water_Voice";
    [Range(0f, 1f)] public float waterSuccessSfxVolume = 1f;
    public float waterSuccessSfxPitch = 1f;

    [Header("Cast Success Voice - Earth")]
    public string earthSuccessSfxName = "Earth_Voice";
    [Range(0f, 1f)] public float earthSuccessSfxVolume = 1f;
    public float earthSuccessSfxPitch = 1f;

    [Header("Cast Success Voice - Thunder")]
    public string thunderSuccessSfxName = "Thunder_Voice";
    [Range(0f, 1f)] public float thunderSuccessSfxVolume = 1f;
    public float thunderSuccessSfxPitch = 1f;

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
    public Vector3 qSpawnOffset = new Vector3(0f, -0.4f, 0f);

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

    [Header("Y - Test Fail")]
    public int yMpCost = 20;
    public float yCooldown = 12f;

    private float qRemain;
    private float eRemain;
    private float rRemain;
    private float tRemain;
    private float yRemain;

    public enum SkillSlot { None, Q, E, R, T, Y }

    private SkillSlot pendingSlot = SkillSlot.None;

    // 이제 마법은 조준 대기 상태를 만들지 않고, 키를 누르는 순간 바로 발동함
    public bool IsAiming => false;

    void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();

        if (aimCamera == null)
            aimCamera = Camera.main;
    }

    void Update()
    {
        TickCooldowns();

        if (PauseMenuUI.IsPaused) return;
        if (StatsPanelToggle.UIBlocked) return;
        if (playerStats == null) return;

        // 기존 방식: 스킬 키 입력 -> 좌클릭으로 발동
        // 변경 방식: 스킬 키를 누르는 순간 바로 발동
        if (skill1Key != KeyCode.None && Input.GetKeyDown(skill1Key))
            CastImmediate(SkillSlot.Q);
        else if (skill2Key != KeyCode.None && Input.GetKeyDown(skill2Key))
            CastImmediate(SkillSlot.E);
        else if (skill3Key != KeyCode.None && Input.GetKeyDown(skill3Key))
            CastImmediate(SkillSlot.R);
        else if (skill4Key != KeyCode.None && Input.GetKeyDown(skill4Key))
            CastImmediate(SkillSlot.T);
        else if (skill5Key != KeyCode.None && Input.GetKeyDown(skill5Key))
            CastImmediate(SkillSlot.Y);
    }

    public void SelectSkill(SkillSlot slot)
    {
        // 음성 인식이나 다른 스크립트에서 SelectSkill을 호출해도
        // 이제는 선택 상태로 기다리지 않고 바로 발동함
        CastImmediate(slot);
    }

    private void CastImmediate(SkillSlot slot)
    {
        pendingSlot = SkillSlot.None;

        switch (slot)
        {
            case SkillSlot.Q:
                TryCastQ();
                break;
            case SkillSlot.E:
                TryCastE();
                break;
            case SkillSlot.R:
                TryCastR();
                break;
            case SkillSlot.T:
                TryCastT();
                break;
            case SkillSlot.Y:
                TryCastY();
                break;
        }
    }

    private void ConfirmCast()
    {
        SkillSlot toCast = pendingSlot;
        pendingSlot = SkillSlot.None;

        switch (toCast)
        {
            case SkillSlot.Q:
                TryCastQ();
                break;
            case SkillSlot.E:
                TryCastE();
                break;
            case SkillSlot.R:
                TryCastR();
                break;
            case SkillSlot.T:
                TryCastT();
                break;
            case SkillSlot.Y:
                TryCastY();
                break;
        }
    }

    private void CancelCast()
    {
        Debug.Log("[MagicAttack] Cancelled " + pendingSlot);
        pendingSlot = SkillSlot.None;
    }

    private void TickCooldowns()
    {
        if (qRemain > 0f) qRemain -= Time.deltaTime;
        if (eRemain > 0f) eRemain -= Time.deltaTime;
        if (rRemain > 0f) rRemain -= Time.deltaTime;
        if (tRemain > 0f) tRemain -= Time.deltaTime;
        if (yRemain > 0f) yRemain -= Time.deltaTime;
    }

    private bool IsUnlocked(int slotIndex)
    {
        if (SkillProgressionManager.Instance == null)
            return true;

        return SkillProgressionManager.Instance.IsUnlocked(slotIndex);
    }

    private bool TrySpendMP(int amount, string skillName)
    {
        if (playerStats.SpendMP(amount))
            return true;

        Debug.Log(skillName + " MP 부족");
        return false;
    }

    private Quaternion GetFlatLookRotation()
    {
        Vector3 forward = aimCamera != null ? aimCamera.transform.forward : transform.forward;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up);

        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private Vector3 GetDefaultSpawnPosition()
    {
        Quaternion rot = GetFlatLookRotation();

        return transform.position
               + Vector3.up * spawnHeightOffset
               + rot * Vector3.forward * spawnForwardOffset;
    }

    private Vector3 GetCastEffectPosition()
    {
        if (castEffectPoint != null)
            return castEffectPoint.position;

        return transform.position + Vector3.up * spawnHeightOffset;
    }

    private bool TryGetAimPoint(out Vector3 point)
    {
        Ray ray = aimCamera != null
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

    private void PlayResultParticle(ParticleType type, Vector3 pos, Quaternion rot)
    {
        if (ParticleManager.Instance == null) return;

        ParticleManager.Instance.Play(type, pos, rot, resultParticleLifetime);
    }

    private void PlayFailSound(Vector3 pos)
    {
        if (SoundManager.Instance == null) return;
        if (string.IsNullOrEmpty(failSfxName)) return;
        if (!SoundManager.Instance.HasSFX(failSfxName)) return;

        SoundManager.Instance.PlaySFX3D(failSfxName, pos, failSfxVolume, failSfxPitch);
    }

    private void PlaySuccessSound(string clipName, Vector3 pos, float volume, float pitch)
    {
        if (SoundManager.Instance == null) return;
        if (string.IsNullOrEmpty(clipName)) return;
        if (!SoundManager.Instance.HasSFX(clipName)) return;

        SoundManager.Instance.PlaySFX3D(clipName, pos, volume, pitch);
    }

    private bool FailCast(string reason, Vector3 pos, Quaternion rot)
    {
        Debug.Log("[MagicAttack] Cast Failed: " + reason);

        PlayResultParticle(failParticle, pos, rot);
        PlayFailSound(pos);

        return false;
    }

    private bool SuccessCast(Vector3 pos, Quaternion rot, string successClipName, float successVolume, float successPitch)
    {
        PlayResultParticle(successParticle, pos, rot);
        PlaySuccessSound(successClipName, pos, successVolume, successPitch);

        return true;
    }

    public bool TryCastQ()
    {
        Quaternion rot = GetFlatLookRotation();

        Vector3 pos = qSpawnPoint != null ? qSpawnPoint.position : GetDefaultSpawnPosition();
        pos += transform.TransformDirection(qSpawnOffset);

        if (!IsUnlocked(0)) return FailCast("Q 잠금 상태", pos, rot);
        if (qRemain > 0f) return FailCast("Q 쿨타임", pos, rot);
        if (qProjectilePrefab == null) return FailCast("Q 프리팹 없음", pos, rot);
        if (!TrySpendMP(qMpCost, "Q")) return FailCast("Q MP 부족", pos, rot);

        GameObject go = Instantiate(qProjectilePrefab, pos, rot);

        SkillProjectile projectile = go.GetComponent<SkillProjectile>();

        if (projectile == null)
        {
            Debug.LogWarning("Q 프리팹에 SkillProjectile이 없음");
            Destroy(go);
            return FailCast("Q SkillProjectile 없음", pos, rot);
        }

        int damage = playerStats.GetMagicDamage(qBaseDamage);

        projectile.Launch(damage, playerStats, enemyMask, qProjectileSpeed, qMaxDistance);

        qRemain = qCooldown;

        return SuccessCast(pos, rot, fireballSuccessSfxName, fireballSuccessSfxVolume, fireballSuccessSfxPitch);
    }

    public bool TryCastE()
    {
        TryGetAimPoint(out Vector3 point);

        Vector3 fxPos = GetCastEffectPosition();
        Quaternion fxRot = GetFlatLookRotation();

        if (!IsUnlocked(1)) return FailCast("E 잠금 상태", fxPos, fxRot);
        if (eRemain > 0f) return FailCast("E 쿨타임", fxPos, fxRot);
        if (eWaterFieldPrefab == null) return FailCast("E 프리팹 없음", fxPos, fxRot);
        if (!TrySpendMP(eMpCost, "E")) return FailCast("E MP 부족", fxPos, fxRot);

        GameObject go = Instantiate(eWaterFieldPrefab, point + Vector3.up * 0.05f, Quaternion.identity);

        WaterFieldSkill water = go.GetComponent<WaterFieldSkill>();

        if (water == null)
        {
            Debug.LogWarning("E 프리팹에 WaterFieldSkill이 없음");
            Destroy(go);
            return FailCast("E WaterFieldSkill 없음", fxPos, fxRot);
        }

        int tickDamage = playerStats.GetMagicDamage(eBaseTickDamage);

        water.Init(
            playerStats,
            enemyMask,
            tickDamage,
            eDuration,
            eTickInterval,
            eSlowMultiplier,
            eSlowDuration
        );

        eRemain = eCooldown;

        return SuccessCast(fxPos, fxRot, waterSuccessSfxName, waterSuccessSfxVolume, waterSuccessSfxPitch);
    }

    public bool TryCastR()
    {
        if (!TryGetAimPoint(out Vector3 point))
            point = transform.position;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

        Vector3 fxPos = GetCastEffectPosition();
        Quaternion fxRot = GetFlatLookRotation();

        if (!IsUnlocked(2)) return FailCast("R 잠금 상태", fxPos, fxRot);
        if (rRemain > 0f) return FailCast("R 쿨타임", fxPos, fxRot);
        if (rEarthWallPrefab == null) return FailCast("R 프리팹 없음", fxPos, fxRot);
        if (!TrySpendMP(rMpCost, "R")) return FailCast("R MP 부족", fxPos, fxRot);

        GameObject go = Instantiate(rEarthWallPrefab, point, rot);

        EarthWallSkill wall = go.GetComponent<EarthWallSkill>();

        if (wall == null)
        {
            Debug.LogWarning("R 프리팹에 EarthWallSkill이 없음");
            Destroy(go);
            return FailCast("R EarthWallSkill 없음", fxPos, fxRot);
        }

        int damage = playerStats.GetMagicDamage(rBaseDamage);

        wall.Init(playerStats, enemyMask, damage, rLifeTime, transform.root);

        rRemain = rCooldown;

        return SuccessCast(fxPos, fxRot, earthSuccessSfxName, earthSuccessSfxVolume, earthSuccessSfxPitch);
    }

    public bool TryCastT()
    {
        TryGetAimPoint(out Vector3 point);

        Vector3 fxPos = GetCastEffectPosition();
        Quaternion fxRot = GetFlatLookRotation();

        if (!IsUnlocked(3)) return FailCast("T 잠금 상태", fxPos, fxRot);
        if (tRemain > 0f) return FailCast("T 쿨타임", fxPos, fxRot);
        if (tThunderRainPrefab == null) return FailCast("T 프리팹 없음", fxPos, fxRot);
        if (!TrySpendMP(tMpCost, "T")) return FailCast("T MP 부족", fxPos, fxRot);

        GameObject go = Instantiate(tThunderRainPrefab, point + Vector3.up * 0.05f, Quaternion.identity);

        ThunderRainSkill thunder = go.GetComponent<ThunderRainSkill>();

        if (thunder == null)
        {
            Debug.LogWarning("T 프리팹에 ThunderRainSkill이 없음");
            Destroy(go);
            return FailCast("T ThunderRainSkill 없음", fxPos, fxRot);
        }

        int damage = playerStats.GetMagicDamage(tBaseDamagePerStrike);

        thunder.Init(
            playerStats,
            enemyMask,
            damage,
            tAreaRadius,
            tSingleStrikeRadius,
            tStrikeCount,
            tWarningDuration,
            tTotalStrikeDuration
        );

        tRemain = tCooldown;

        return SuccessCast(fxPos, fxRot, thunderSuccessSfxName, thunderSuccessSfxVolume, thunderSuccessSfxPitch);
    }

    public bool TryCastY()
    {
        Vector3 pos = GetCastEffectPosition();
        Quaternion rot = GetFlatLookRotation();

        return FailCast("Y 테스트 실패", pos, rot);
    }

    public float GetCooldownRatio(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0:
                return qCooldown <= 0f ? 0f : Mathf.Clamp01(qRemain / qCooldown);
            case 1:
                return eCooldown <= 0f ? 0f : Mathf.Clamp01(eRemain / eCooldown);
            case 2:
                return rCooldown <= 0f ? 0f : Mathf.Clamp01(rRemain / rCooldown);
            case 3:
                return tCooldown <= 0f ? 0f : Mathf.Clamp01(tRemain / tCooldown);
            case 4:
                return yCooldown <= 0f ? 0f : Mathf.Clamp01(yRemain / yCooldown);
        }

        return 0f;
    }

    public float GetCooldownDuration(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0:
                return qCooldown;
            case 1:
                return eCooldown;
            case 2:
                return rCooldown;
            case 3:
                return tCooldown;
            case 4:
                return yCooldown;
        }

        return 0f;
    }
}