using UnityEngine;

[CreateAssetMenu(menuName = "Boss/GolemData")]
public class GolemData : BossData
{
    [Header("Golem Specific")]
    public float OverheatInterval = 10f;
    public float VentingDuration = 5f;
    public float OverloadDuration = 5f;

    [Header("Smash Pattern")]
    public float SmashDamage = 20f;
    public float SmashRadius = 5f;     // 공격 범위 (반지름)
    public float SmashCastTime = 1.5f; // 전조 시간 (기가 모이는 시간)
    public float KnockbackForce = 10f; // 밀쳐내는 힘
    public float SmashCooldown = 8.0f;

    [Header("Throw Pattern")]
    public GameObject RockPrefab;   // 던질 바위 프리팹
    public float ThrowDamage = 15f;
    public float ThrowSpeed = 15f;
    public float ThrowCastTime = 1.0f; // 돌을 집어 드는 시간
    public float ThrowCooldown = 5.0f; // 자주 던지지 않게 별도 쿨타임

    [Header("Rush Pattern")]
    public float RushChargeTime = 1.0f; // 돌진 준비 시간 (조준)
    public float RushSpeed = 20f;       // 돌진 속도 (평소보다 훨씬 빨라야 함)
    public float RushDamage = 15f;
    public float RushKnockback = 15f;   // 튕겨내는 힘
    public float RushDistanceError = 1.0f; // 목표 지점 도달 인정 오차
    public float RushCooldown = 10.0f;
}