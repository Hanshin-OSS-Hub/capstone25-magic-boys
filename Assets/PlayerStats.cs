using UnityEngine;
using System;

public enum StatType { STR, DEX, MAG, LUK }

public class PlayerStats : MonoBehaviour
{
    [Header("Base")]
    public int baseMaxHP = 100;
    public int baseMaxMP = 50;

    [Header("Runtime")]
    public int maxHP;
    public int currentHP;
    public int maxMP;
    public int currentMP;

    [Header("EXP / Level")]
    public int level = 1;
    public int currentExp = 0;
    public int expToNext = 50;

    [Header("Stats (for damage)")]
    public int STR = 0;   // 물리 보정(옵션)
    public int MAG = 0;   // 마법 보정
    public int LUK = 0;   // 크리 확률/배수 보정

    [Header("Crit Settings")]
    [Range(0f, 1f)] public float baseCritChance = 0.05f;   // 5%
    public float baseCritMultiplier = 1.5f;               // 1.5x

    // UI 이벤트
    public event Action<int, int> OnHPChanged;
    public event Action<int, int> OnMPChanged;
    public event Action<int, int, int> OnExpChanged;
    public event Action OnDied;

    void Awake()
    {
        maxHP = baseMaxHP; currentHP = maxHP;
        maxMP = baseMaxMP; currentMP = maxMP;
        BroadcastAll();
    }

    void BroadcastAll()
    {
        OnHPChanged?.Invoke(currentHP, maxHP);
        OnMPChanged?.Invoke(currentMP, maxMP);
        OnExpChanged?.Invoke(currentExp, expToNext, level);
    }

    // ---- HP ----
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHP = Mathf.Max(0, currentHP - amount);
        OnHPChanged?.Invoke(currentHP, maxHP);
        if (currentHP <= 0) Die();
    }
    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    // ---- MP ----
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

    // ---- EXP / Level ----
    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        currentExp += amount;
        while (currentExp >= expToNext)
        {
            currentExp -= expToNext;
            level++;
            expToNext = Mathf.RoundToInt(expToNext * 1.2f); // 간단 스케일
        }
        OnExpChanged?.Invoke(currentExp, expToNext, level);
    }

    void Die()
    {
        Debug.Log("Player Died");
        OnDied?.Invoke();
    }

    // ====== Damage helpers ======
    // 물리 공격 계산(원하면 좌클릭 근접/원거리 기본 공격에 사용)
    public int GetPhysicalDamage(int baseWeaponOrSkill = 0)
    {
        float dmg = baseWeaponOrSkill + STR * 2f;            // 예시 스케일
        ApplyCrit(ref dmg);
        return Mathf.Max(0, Mathf.RoundToInt(dmg));
    }

    // 마법 공격 계산(스킬1 등에서 사용)
    public int GetMagicDamage(int baseSkill = 0)
    {
        float dmg = baseSkill + MAG * 3f;                    // 예시 스케일
        ApplyCrit(ref dmg);
        return Mathf.Max(0, Mathf.RoundToInt(dmg));
    }

    void ApplyCrit(ref float dmg)
    {
        float critChance = Mathf.Clamp01(baseCritChance + LUK * 0.005f);
        float critMult = baseCritMultiplier + LUK * 0.01f;

        if (UnityEngine.Random.value < critChance) // ★ 여기!
            dmg *= critMult;
    }
}