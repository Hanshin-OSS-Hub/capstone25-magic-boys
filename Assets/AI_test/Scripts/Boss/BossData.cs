using UnityEngine;

[CreateAssetMenu(menuName = "Boss/BossData")]
public class BossData : ScriptableObject
{
    [Header("Common Stats")]
    public string BossName;
    public float MaxHP;
    public float MoveSpeed;
    public float Damage;
    public float DetectionRange;
    public float AttackRange;
    public float AttackCooldown;


}