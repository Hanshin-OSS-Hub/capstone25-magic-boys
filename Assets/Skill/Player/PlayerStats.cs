using UnityEngine;
using System;

public enum StatType { STR, DEX, MAG, LUK }

public class PlayerStats : MonoBehaviour, IDamageable // 데미지를 받을 수 있는 객체에 IDamageble 인터페이스를 넣어야함
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

    void Awake()
    {
        RecalculateDerived();
        currentHP = maxHP;
        currentMP = maxMP;
        BroadcastAll();
    }

    void Update()
    {
        TickMPRegen(Time.deltaTime);
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
    public void TakeDamage(float amount)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Max(0, currentHP - (int)amount); // 원래 인터페이스를 float로 설계를 했는데 임시로 형변환해서 int로 처리할게요(성민)
        OnHPChanged?.Invoke(currentHP, maxHP);
        if (currentHP <= 0) Die();
    }
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public bool SpendMP(int amount)
    {
        if (amount <= 0) return true;
        if (currentMP < amount) return false;
        currentMP -= amount;
        OnMPChanged?.Invoke(currentMP, maxMP);
        return true;
    }
    public void RestoreMP(int amount)
    {
        currentMP = Mathf.Min(maxMP, currentMP + amount);
        OnMPChanged?.Invoke(currentMP, maxMP);
    }
    void TickMPRegen(float dt)
    {
        if (mpRegenPerSec <= 0f || currentMP >= maxMP) return;
        _mpRegenCarry += mpRegenPerSec * dt;
        int gain = Mathf.FloorToInt(_mpRegenCarry);
        if (gain > 0)
        {
            _mpRegenCarry -= gain;
            RestoreMP(gain);
        }
    }

    // ===== EXP / Level =====
    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        currentExp += amount;
        while (currentExp >= expToNext)
        {
            currentExp -= expToNext;
            level++;
            statPoints += 1; // ★ 레벨업 시 포인트 지급
            expToNext = Mathf.RoundToInt(expToNext * 1.2f);
            OnStatPointChanged?.Invoke(statPoints);
        }
        OnExpChanged?.Invoke(currentExp, expToNext, level);
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
}