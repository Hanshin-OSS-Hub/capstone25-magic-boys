using UnityEngine;
using UnityEngine.EventSystems;

public class TestPlayerAttack : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public Transform firePoint;
    public PlayerStats player;

    [Header("Layers")]
    public LayerMask enemyMask;
    public LayerMask worldMask;

    [Header("Ranged (LMB)")]
    public float rangedRange = 25f;
    public int rangedDamage = 10;

    [Header("Melee (F)")]
    public float meleeRange = 2.5f;
    public float meleeRadius = 0.8f;
    public int meleeDamage = 20;

    [Header("Skill #1 (Key 1)")]
    public KeyCode skillKey = KeyCode.Alpha1;
    public int skillMpCost = 15;
    public int baseSkillDamage = 30;
    public SkillProjectile projectilePrefab;
    public float projectileSpeed = 22f;
    public float projectileMaxDistance = 40f;

    [Header("Debug")]
    public bool drawDebug = true;
    public Color debugRangedColor = Color.cyan;
    public Color debugMeleeColor = Color.yellow;

    void Awake()
    {
        if (!cam) cam = Camera.main;
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
        // ★ 메뉴 열렸으면 입력 전부 차단 (Time.timeScale=0이어도 Update는 돎)
        if (StatsPanelToggle.UIBlocked) return;

        // ★ UI 위 클릭이면 전투 입력 차단
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // ========== 입력 ==========

        if (Input.GetMouseButtonDown(0))
            ShootRangedFromCharacter();

        if (Input.GetKeyDown(KeyCode.F))
            DoMelee();

        if (Input.GetKeyDown(skillKey))
            CastSkill1();
    }

    // ===== 조준 유틸 =====
    Vector3 GetAimPoint(float maxDistance = 1000f)
    {
        Ray aimRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(aimRay, out var hit, maxDistance, worldMask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return aimRay.origin + aimRay.direction * maxDistance;
    }

    // ===== 원거리 기본 공격 (LMB) =====
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

    // ===== 근접 (F) =====
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

    // ===== 스킬 1 (Key 1) : MP 소모 + 투사체 =====
    void CastSkill1()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("SkillProjectile 프리팹이 비어있습니다.");
            return;
        }

        if (!player.SpendMP(skillMpCost))
        {
            Debug.Log("MP가 부족합니다!");
            return;
        }

        Vector3 aimPoint = GetAimPoint(projectileMaxDistance);
        Vector3 origin = firePoint.position;
        Vector3 dir = (aimPoint - origin).normalized;

        int finalDamage = player.GetMagicDamage(baseSkillDamage);

        var proj = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));
        proj.Launch(finalDamage, player, enemyMask, projectileSpeed, projectileMaxDistance);
    }
}