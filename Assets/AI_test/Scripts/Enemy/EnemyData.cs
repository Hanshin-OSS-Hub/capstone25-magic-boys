using UnityEngine;

public enum EnemyCombatType // 전투 스타일 종류
{
    Melee, // 근접
    Ranged // 원거리
}



[CreateAssetMenu(menuName = "Enemy/EnemyData")] // 에셋파일 생성 항목에 Enemy/EnemyDate 추가
public class EnemyData : ScriptableObject
{
    public string EnemyName;
    [TextArea] // 인스펙터창에 여러줄 텍스트 입력 가능
    public string EnemyDescription;

    [Header("Combat Settings")]
    public EnemyCombatType CombatType;

    public float MaxHP;
    public float Damage;
    public float MoveSpeed;
    public float DetectionRange;
    public float AttackRange;
    public float AttackCooldown;

    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;

    [Header("Reward")]
    public int DropExp = 10;

}
