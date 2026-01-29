using UnityEngine;
using System;

public enum StatType { STR, DEX, MAG, LUK }

public class PlayerStats : MonoBehaviour
{
    [Header("Base (before stat scaling)")]
    public int baseMaxHP = 100;
    public int baseMaxMP = 50;
    public int basePhysicalATK = 10;
    public int baseMagicATK = 10;
    public float baseMPRegenPerSec = 1f;
    public float baseMoveSpeed = 5f;          // m/s
    public float baseAttacksPerSecond = 1.5f; // 초당 공격수 (공격속도)

    [Header("Runtime Resources")]
    public int maxHP;
    public int currentHP;
    public int maxMP;
    public int currentMP;

    [Header("EXP / Level / Stat Points")]
    public int level = 1;
    public int currentExp = 0;
    public int expToNext = 50;
    public int statPoints = 0;

    [Header("Allocated Stats")]
    public int STR = 0;  // 힘: HP/물리공격 ↑
    public int DEX = 0;  // 민첩: 공격속도/이동속도 ↑
    public int MAG = 0;  // 마력: MP최대/자연회복/마법공격 ↑
    public int LUK = 0;  // 운: 크리확률/크리배수 ↑

    [Header("Derived (auto)")]
    public int physicalATK;
    public int magicATK;
    public float mpRegenPerSec;
    public float moveSpeed;
    public float attacksPerSecond;
    public float attackInterval; // 1 / APS
    [Range(0, 1)] public float critChance;
    public float critMultiplier;

    [Header("Crit Base")]
    [Range(0, 1)] public float baseCritChance = 0.05f;  // 5%
    public float baseCritMultiplier = 1.5f;            // 1.5x

    // Events (UI 등)
    public event Action<int, int> OnHPChanged;
    public event Action<int, int> OnMPChanged;
    public event Action<int, int, int> OnExpChanged;
    public event Action<int> OnStatPointChanged;
    public event Action OnDied;

    float _mpRegenCarry;

    // ===== PlayerPrefs Keys =====
    const string K_LV = "PS_LV";
    const string K_EXP = "PS_EXP";
    const string K_EXP_NEXT = "PS_EXP_NEXT";
    const string K_SP = "PS_SP";
    const string K_STR = "PS_STR";
    const string K_DEX = "PS_DEX";
    const string K_MAG = "PS_MAG";
    const string K_LUK = "PS_LUK";
    const string K_HP = "PS_HP";
    const string K_MP = "PS_MP";

    void Awake()
    {
        // ✅ 먼저 로드 시도
        bool loaded = Load();

        // 로드 실패(새 게임)면 기본 세팅
        if (!loaded)
        {
            RecalculateDerived();
            currentHP = maxHP;
            currentMP = maxMP;
            BroadcastAll();
        }
    }

    void Update()
    {
        TickMPRegen(Time.deltaTime);
    }

    void OnApplicationQuit()
    {
        Save();
    }

    void OnDisable()
    {
        // 씬 전환/비활성에서도 저장(원치 않으면 주석 처리)
        Save();
    }

    void BroadcastAll()
    {
        OnHPChanged?.Invoke(currentHP, maxHP);
        OnMPChanged?.Invoke(currentMP, maxMP);
        OnExpChanged?.Invoke(currentExp, expToNext, level);
        OnStatPointChanged?.Invoke(statPoints);
    }

    // ===== Derived rules (간단 밸런스) =====
    public void RecalculateDerived()
    {
        maxHP = baseMaxHP + STR * 10;
        maxMP = baseMaxMP + MAG * 10;

        physicalATK = basePhysicalATK + STR * 2;
        magicATK = baseMagicATK + MAG * 3;

        mpRegenPerSec = baseMPRegenPerSec + MAG * 0.3f;

        moveSpeed = baseMoveSpeed + DEX * 0.15f; // 0.15 m/s per DEX
        attacksPerSecond = baseAttacksPerSecond + DEX * 0.05f; // +0.05 APS per DEX
        attacksPerSecond = Mathf.Max(0.2f, attacksPerSecond);
        attackInterval = 1f / attacksPerSecond;

        critChance = Mathf.Clamp01(baseCritChance + LUK * 0.005f); // +0.5%/LUK
        critMultiplier = baseCritMultiplier + LUK * 0.01f;         // +1%/LUK

        currentHP = Mathf.Min(currentHP, maxHP);
        currentMP = Mathf.Min(currentMP, maxMP);
    }

    // ===== HP / MP =====
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Max(0, currentHP - amount);
        OnHPChanged?.Invoke(currentHP, maxHP);

        Save(); //변화 시 저장(원치 않으면 주석)

        if (currentHP <= 0) Die();
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHPChanged?.Invoke(currentHP, maxHP);
        Save();
    }

    public bool SpendMP(int amount)
    {
        if (amount <= 0) return true;
        if (currentMP < amount) return false;
        currentMP -= amount;
        OnMPChanged?.Invoke(currentMP, maxMP);
        Save();
        return true;
    }

    public void RestoreMP(int amount)
    {
        currentMP = Mathf.Min(maxMP, currentMP + amount);
        OnMPChanged?.Invoke(currentMP, maxMP);
        Save();
    }

    void TickMPRegen(float dt)
    {
        if (mpRegenPerSec <= 0f || currentMP >= maxMP) return;
        _mpRegenCarry += mpRegenPerSec * dt;
        int gain = Mathf.FloorToInt(_mpRegenCarry);
        if (gain > 0)
        {
            _mpRegenCarry -= gain;
            // 자연회복은 저장 너무 잦을 수 있으니 Save() 안 함
            currentMP = Mathf.Min(maxMP, currentMP + gain);
            OnMPChanged?.Invoke(currentMP, maxMP);
        }
    }

    // ===== EXP / Level =====
    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        currentExp += amount;

        bool leveledUp = false;

        while (currentExp >= expToNext)
        {
            currentExp -= expToNext;
            level++;
            statPoints += 1; // 레벨업 시 포인트 지급
            expToNext = Mathf.RoundToInt(expToNext * 1.2f);
            OnStatPointChanged?.Invoke(statPoints);
            leveledUp = true;
        }

        OnExpChanged?.Invoke(currentExp, expToNext, level);

        if (leveledUp) Save(); // 레벨업 같은 큰 변화에서 저장
    }

    void Die()
    {
        Debug.Log("Player Died");
        OnDied?.Invoke();
    }

    // ===== Stat allocation =====
    public bool AllocateStat(StatType type)
    {
        if (statPoints <= 0) return false;

        switch (type)
        {
            case StatType.STR: STR++; break;
            case StatType.DEX: DEX++; break;
            case StatType.MAG: MAG++; break;
            case StatType.LUK: LUK++; break;
        }

        statPoints--;

        RecalculateDerived();
        OnStatPointChanged?.Invoke(statPoints);
        OnHPChanged?.Invoke(currentHP, maxHP);
        OnMPChanged?.Invoke(currentMP, maxMP);

        Save(); // ✅ 스탯 분배는 저장 필수
        return true;
    }

    // ===== Damage helpers =====
    public int GetPhysicalDamage(int baseWeaponOrSkill = 0)
    {
        float dmg = baseWeaponOrSkill + physicalATK;
        ApplyCrit(ref dmg);
        return Mathf.Max(0, Mathf.RoundToInt(dmg));
    }

    public int GetMagicDamage(int baseSkill = 0)
    {
        float dmg = baseSkill + magicATK;
        ApplyCrit(ref dmg);
        return Mathf.Max(0, Mathf.RoundToInt(dmg));
    }

    void ApplyCrit(ref float dmg)
    {
        if (UnityEngine.Random.value < critChance)
            dmg *= critMultiplier;
    }

    // ===== Save / Load =====
    public void Save()
    {
        PlayerPrefs.SetInt(K_LV, level);
        PlayerPrefs.SetInt(K_EXP, currentExp);
        PlayerPrefs.SetInt(K_EXP_NEXT, expToNext);
        PlayerPrefs.SetInt(K_SP, statPoints);

        PlayerPrefs.SetInt(K_STR, STR);
        PlayerPrefs.SetInt(K_DEX, DEX);
        PlayerPrefs.SetInt(K_MAG, MAG);
        PlayerPrefs.SetInt(K_LUK, LUK);

        PlayerPrefs.SetInt(K_HP, currentHP);
        PlayerPrefs.SetInt(K_MP, currentMP);

        PlayerPrefs.Save();
    }

    public bool Load()
    {
        if (!PlayerPrefs.HasKey(K_LV)) return false;

        level = PlayerPrefs.GetInt(K_LV, 1);
        currentExp = PlayerPrefs.GetInt(K_EXP, 0);
        expToNext = PlayerPrefs.GetInt(K_EXP_NEXT, 50);
        statPoints = PlayerPrefs.GetInt(K_SP, 0);

        STR = PlayerPrefs.GetInt(K_STR, 0);
        DEX = PlayerPrefs.GetInt(K_DEX, 0);
        MAG = PlayerPrefs.GetInt(K_MAG, 0);
        LUK = PlayerPrefs.GetInt(K_LUK, 0);

        RecalculateDerived();

        currentHP = Mathf.Clamp(PlayerPrefs.GetInt(K_HP, maxHP), 0, maxHP);
        currentMP = Mathf.Clamp(PlayerPrefs.GetInt(K_MP, maxMP), 0, maxMP);

        BroadcastAll();
        return true;
    }

    // (선택) 새 게임 초기화용
    public static void ResetSavedData()
    {
        PlayerPrefs.DeleteKey(K_LV);
        PlayerPrefs.DeleteKey(K_EXP);
        PlayerPrefs.DeleteKey(K_EXP_NEXT);
        PlayerPrefs.DeleteKey(K_SP);
        PlayerPrefs.DeleteKey(K_STR);
        PlayerPrefs.DeleteKey(K_DEX);
        PlayerPrefs.DeleteKey(K_MAG);
        PlayerPrefs.DeleteKey(K_LUK);
        PlayerPrefs.DeleteKey(K_HP);
        PlayerPrefs.DeleteKey(K_MP);
        PlayerPrefs.Save();
    }
}