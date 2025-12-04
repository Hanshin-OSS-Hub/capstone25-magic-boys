using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyStateManager : MonoBehaviour, IDamageable
{
    [HideInInspector] public IdleState idleState = new IdleState();
    [HideInInspector] public ChaseState chaseState = new ChaseState();
    [HideInInspector] public DeadState deadState = new DeadState();

    public IEnemyState attackState;
    public IEnemyState currentState;

    public EnemyData stats;      // MaxHP, MoveSpeed, DropExp, CombatType 등
    public Transform firePoint;

    public float currentHP;
    public float distanceToPlayer;
    public float attackTimer;

    [HideInInspector] public NavMeshAgent navMeshAgent;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Transform playerTransform;

    // ▼ MeshRenderer → Renderer(스킨 메쉬 포함)로 범용화
    private Renderer rend;
    private Material originalMaterial;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 자식까지 탐색해서 첫 Renderer 확보
        rend = GetComponentInChildren<Renderer>();
        if (rend != null) originalMaterial = rend.material;
    }

    void Start()
    {
        if (stats != null)
        {
            navMeshAgent.speed = stats.MoveSpeed;
            currentHP = stats.MaxHP;
            SetupAttackState();
        }

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) playerTransform = playerObj.transform;

        TransitionToState(idleState);
    }

    void Update()
    {
        distanceToPlayer = playerTransform ? Vector3.Distance(transform.position, playerTransform.position)
                                           : Mathf.Infinity;

        currentState?.UpdateState(this);
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
    }

    public void TransitionToState(IEnemyState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    void SetupAttackState()
    {
        attackState = (stats.CombatType == EnemyCombatType.Ranged)
            ? new RangeAttackState()
            : new AttackState();
    }

    public void OnAttackAnimationFinished()
    {
        TransitionToState(chaseState);
    }

    // === IDamageable ===
    public void TakeDamage(float damage)
    {
        if (currentHP <= 0) return;

        TriggerHitVisual();
        currentHP -= damage;

        if (currentHP <= 0)
        {
            GiveExpToPlayer();
            TransitionToState(deadState);
        }
    }

    public void TriggerHitVisual()
    {
        var hitMat = Resources.Load<Material>("HitMaterial");
        if (hitMat != null) StartCoroutine(HitFlashCoroutine(hitMat));
    }

    IEnumerator HitFlashCoroutine(Material hitMat)
    {
        if (rend != null)
        {
            rend.material = hitMat;
            yield return new WaitForSeconds(0.2f);
            rend.material = originalMaterial;
        }
    }

    public void StartDeathSequence()
    {
        StartCoroutine(DeathCoroutine());
    }

    IEnumerator DeathCoroutine()
    {
        float timer = 0f;
        float sinkTime = 3f;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot * Quaternion.Euler(0, 0, 90);

        while (timer < sinkTime)
        {
            transform.Translate(Vector3.down * 0.5f * Time.deltaTime, Space.World);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, timer / sinkTime);
            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    public void ResetEnemy()
    {
        currentHP = stats.MaxHP;
        navMeshAgent.enabled = true;
        navMeshAgent.isStopped = true;
        var col = GetComponent<Collider>();
        if (col) col.enabled = true;

        TransitionToState(idleState);
    }

    // ▼ EXP 지급: PlayerStats로 변경 (SimplePlayerMover → PlayerStats)
    void GiveExpToPlayer()
    {
        if (!playerTransform) return;
        var ps = playerTransform.GetComponent<PlayerStats>();
        if (ps != null) ps.AddExp(stats.DropExp);
    }
}