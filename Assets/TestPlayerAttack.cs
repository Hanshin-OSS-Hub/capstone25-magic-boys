using UnityEngine;

public class TestPlayerAttack : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                 // 메인 카메라(비워두면 자동)
    public Transform firePoint;        // 발사 원점(손/무기 끝)
    public PlayerStats player;         // 킬 크레딧/MP/데미지 출처

    [Header("Layers")]
    public LayerMask enemyMask;        // Enemy만
    public LayerMask worldMask;        // 벽/지형 등(에임 점 잡을 때)

    [Header("Ranged (LMB)")]
    public float rangedRange = 25f;
    public int rangedDamage = 10;

    [Header("Melee (F)")]
    public float meleeRange = 2.5f;
    public float meleeRadius = 0.8f;
    public int meleeDamage = 20;

    [Header("Skill #1 (Key 1)")]
    public KeyCode skillKey = KeyCode.Alpha1;
    public int skillMpCost = 15;                 // ★ 필요한 MP
    public int baseSkillDamage = 30;             // 마법 계수(최종은 PlayerStats.GetMagicDamage로 계산)
    public SkillProjectile projectilePrefab;     // ★ 투사체 프리팹
    public float projectileSpeed = 22f;          // 투사체 속도
    public float projectileMaxDistance = 40f;    // 최대 비행거리

    [Header("Debug")]
    public bool drawDebug = true;
    public Color debugRangedColor = Color.cyan;
    public Color debugMeleeColor = Color.yellow;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!cam) Debug.LogError("TestPlayerAttack: Camera 필요!");

        if (!firePoint)
        {
            var temp = new GameObject("~FirePointAuto");
            temp.transform.SetParent(transform);
            temp.transform.localPosition = new Vector3(0f, 1.5f, 0.2f);
            firePoint = temp.transform;
        }
    }

    void Update()
    {
        // 기본 공격
        if (Input.GetMouseButtonDown(0)) ShootRangedFromCharacter();
        if (Input.GetKeyDown(KeyCode.F)) DoMelee();

        // ★ 스킬 1: 투사체 발사 + MP 차감
        if (Input.GetKeyDown(skillKey)) CastSkill1();
    }

    // 화면 중앙 에임 포인트
    Vector3 GetAimPoint(float maxDistance = 1000f)
    {
        Ray aimRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        // 월샷 방지: 먼저 월드(벽/지형)에 맞으면 그 지점 사용
        if (Physics.Raycast(aimRay, out var hit, maxDistance, worldMask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return aimRay.origin + aimRay.direction * maxDistance;
    }

    void ShootRangedFromCharacter()
    {
        Vector3 aimPoint = GetAimPoint(rangedRange * 2f);
        Vector3 origin = firePoint.position;
        Vector3 dir = (aimPoint - origin).normalized;

        if (drawDebug) Debug.DrawRay(origin, dir * rangedRange, debugRangedColor, 0.25f);

        if (Physics.Raycast(origin, dir, out var hit, rangedRange, enemyMask, QueryTriggerInteraction.Collide))
        {
            var enemy = hit.collider.GetComponentInParent<EnemySimple>() ?? hit.collider.GetComponent<EnemySimple>();
            if (enemy)
            {
                Debug.Log($"적이 맞았다: {enemy.name}");
                enemy.TakeDamage(rangedDamage, player);
            }
        }
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
                Debug.Log($"근접타격! {enemy.name}");
                enemy.TakeDamage(meleeDamage, player);
            }
        }
    }

    // ★ 스킬 1: MP 확인 → 투사체 생성 → 발사
    void CastSkill1()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("SkillProjectile 프리팹이 비어있습니다.");
            return;
        }

        // MP 체크 & 차감
        if (!player.SpendMP(skillMpCost))
        {
            Debug.Log("MP가 부족합니다!");
            return;
        }

        // 발사 방향 = firePoint → 화면 중앙 에임 지점
        Vector3 aimPoint = GetAimPoint(projectileMaxDistance);
        Vector3 origin = firePoint.position;
        Vector3 dir = (aimPoint - origin).normalized;

        // 데미지는 MAG/LUK 반영 (기대 크리 포함)
        int finalDamage = player.GetMagicDamage(baseSkillDamage);

        // 투사체 생성 및 초기화
        var proj = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));
        proj.Launch(finalDamage, player, enemyMask, projectileSpeed, projectileMaxDistance);
    }
}