using UnityEngine;

[CreateAssetMenu(menuName = "Enemy/EnemyData")] // 에셋파일 생성 항목에 Enemy/EnemyDate 추가
public class EnemyData : ScriptableObject
{
    public string EnemyName;
    [TextArea] // 인스펙터창에 여러줄 텍스트 입력 가능
    public string EnemyDescription;

    public float MaxHP;
    public float Damage;
    public float MoveSpeed;
    public float DetectionRange;
    public float AttackRange;
    public float AttackCooldown;

}
