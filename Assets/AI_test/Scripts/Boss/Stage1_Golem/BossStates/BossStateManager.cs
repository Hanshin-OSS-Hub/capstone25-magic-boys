using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossStateManager : MonoBehaviour, IDamageable
{
    // [상태 인스턴스]
    public BossChaseState chaseState { get; } = new BossChaseState();
    public BossDeadState deadState { get; } = new BossDeadState();
    public BossAttackState attackState { get; } = new BossAttackState();
    public BossSmashState SmashState { get; } = new BossSmashState();
    public BossThrowState throwState { get; } = new BossThrowState();
    public BossRushState RushState { get; } = new BossRushState();

    // 패턴 상태 (인터페이스)
    public IBossState PatternState1 { get; private set; } // Venting
    public IBossState PatternState2 { get; private set; } // Overload

    public IBossState currentState;

    // [데이터]
    public BossData stats;
    public GameObject weakPointObject;

    public Transform handTransform;
    public float throwCooldownTimer;
    public float rushCooldownTimer;
    public float smashCooldownTimer;

    [Header("Visual Effects")]
    public GameObject smashIndicator;

    // [런타임 변수]
    public float currentHP;
    public float heatTimer;
    public float attackTimer;

    [HideInInspector] public NavMeshAgent navMeshAgent;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Transform playerTransform;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        if (weakPointObject != null) weakPointObject.SetActive(false);
    }

    void Start()
    {
        if (stats != null)
        {
            navMeshAgent.speed = stats.MoveSpeed;
            currentHP = stats.MaxHP;
        }

        if (smashIndicator != null) smashIndicator.SetActive(false);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        InitializeBossPatterns();
        TransitionToState(chaseState);
    }

    void InitializeBossPatterns()
    {
        if (stats is GolemData golemData)
        {
            heatTimer = golemData.OverheatInterval;
            PatternState1 = new VentingState();
            PatternState2 = new OverloadState();
        }
    }

    void Update()
    {
        currentState?.UpdateState(this);

        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (throwCooldownTimer > 0) throwCooldownTimer -= Time.deltaTime;
        if (rushCooldownTimer > 0) rushCooldownTimer -= Time.deltaTime;
        if (smashCooldownTimer > 0) smashCooldownTimer -= Time.deltaTime;

        if (currentState == chaseState)
        {
            heatTimer -= Time.deltaTime;
            if (heatTimer <= 0 && PatternState1 != null)
            {
                TransitionToState(PatternState1);
            }
        }
    }

    public void TransitionToState(IBossState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

 
    public bool SelectNextPattern()
    {
        if (playerTransform == null) return false;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // 데이터 가져오기
        float smashRange = 3.0f;
        float attackRange = 2.5f;

        if (stats is GolemData data)
        {
            smashRange = data.SmashRadius;
            attackRange = data.AttackRange;
        }

        // 1. [스매쉬] 쿨타임 찼고, 거리가 (SmashRadius - 1) 이내일 때
        // (단, 범위가 너무 작아지지 않게 최소값 보정)
        float effectiveSmashRange = Mathf.Max(1.0f, smashRange - 1.0f);
        if (smashCooldownTimer <= 0 && dist <= effectiveSmashRange)
        {
            TransitionToState(SmashState);
            smashCooldownTimer = (stats is GolemData d) ? d.SmashCooldown : 15.0f;
            return true;
        }


        if (dist > attackRange)
        {
            bool canRush = rushCooldownTimer <= 0;
            bool canThrow = throwCooldownTimer <= 0;

            if (canRush && canThrow)
            {
                int dice = Random.Range(0, 100);
                if (dice < 60)
                {
                    TransitionToState(RushState);
                    rushCooldownTimer = (stats is GolemData d) ? d.RushCooldown : 10.0f;
                }
                else
                {
                    TransitionToState(throwState);
                    throwCooldownTimer = (stats is GolemData d) ? d.ThrowCooldown : 8.0f;
                }
                return true;
            }
            else if (canRush)
            {
                TransitionToState(RushState);
                rushCooldownTimer = (stats is GolemData d) ? d.RushCooldown : 10.0f;
                return true;
            }
            else if (canThrow)
            {
                TransitionToState(throwState);
                throwCooldownTimer = (stats is GolemData d) ? d.ThrowCooldown : 8.0f;
                return true;
            }
        }

        // 3. 평타 쿨타임 찼고, 공격 사거리 이내일 때
        // 위의 스킬들을 다 못 쓰는 상황 -> 평타
        if (attackTimer <= 0 && dist <= attackRange)
        {
            TransitionToState(attackState);
            attackTimer = stats.AttackCooldown;
            return true;
        }

        // 아무 스킬도 못 씀
        return false;
    }

    public void OnCoreHit()
    {
        float bonusDamage = stats.MaxHP * 0.1f;
        TakeDamage(bonusDamage);

        if (currentHP > 0 && PatternState2 != null)
        {
            TransitionToState(PatternState2);
        }
    }

    public void OnBossAttackFinished()
    {
        TransitionToState(chaseState);
    }

    public void TakeDamage(float damage)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;
        Debug.Log($"보스 HP: {currentHP}");

        if (currentHP <= 0) TransitionToState(deadState);
    }

    public void StartDeathSequence()
    {
        StartCoroutine(DeathCoroutine());
    }

    private IEnumerator DeathCoroutine()
    {
        yield return new WaitForSeconds(3.0f); // 애니메이션 대기
        Destroy(gameObject);
    }

    public void PerformBasicAttack()
    {
        Vector3 attackPos = transform.position + (transform.forward * 1.5f) + (Vector3.down * 0.5f);
        float attackRadius = 5.0f;

        Collider[] hitColliders = Physics.OverlapSphere(attackPos, attackRadius);
        bool hitSomething = false;
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                IDamageable target = hitCollider.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(stats.Damage);
                    hitSomething = true;
                }
            }
        }
        if (hitSomething) Debug.Log("평타 적중!");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 attackPos = transform.position + (transform.forward * 1.5f) + (Vector3.down * 0.5f);
        Gizmos.DrawWireSphere(attackPos, 5.0f);
    }
}