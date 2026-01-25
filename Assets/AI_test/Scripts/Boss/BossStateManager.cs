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

    // [데이터] (부모 타입인 BossData로 선언)
    public BossData stats;
    public GameObject weakPointObject;

    public Transform handTransform; //  돌이 생성될 손 위치
    public float throwCooldownTimer; // 투척 전용 쿨타임
    public float rushCooldownTimer;
    public float smashCooldownTimer;

    [Header("Visual Effects")]
    public GameObject smashIndicator; //  공격 범위 표시용 빨간 장판


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
            // heatTimer 초기화는 아래 InitializeBossPatterns에서 함
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
        // 나중에 다른 보스도 else if로 추가
    }

    void Update()
    {
        currentState?.UpdateState(this);

        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (throwCooldownTimer > 0) throwCooldownTimer -= Time.deltaTime;
        if (rushCooldownTimer > 0) rushCooldownTimer -= Time.deltaTime;
        if (smashCooldownTimer > 0) smashCooldownTimer -= Time.deltaTime;

        // 평상시(추적 중)에만 열이 차오름
        if (currentState == chaseState)
        {
            heatTimer -= Time.deltaTime;
            // 열이 다 차면 패턴 1 (Venting) 시작
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

        // 1. [초근접] Smash
        if (dist <= smashRange)
        {
            if (smashCooldownTimer <= 0)
            {
                TransitionToState(SmashState);
                smashCooldownTimer = (stats is GolemData d) ? d.SmashCooldown : 15.0f;
                return true;
            }
        }

        // 2. [근접] 평타
        if (dist <= attackRange)
        {
            if (attackTimer <= 0)
            {
                TransitionToState(attackState);
                attackTimer = stats.AttackCooldown;
                return true; // [수정] 공격 성공!
            }
        }

        // 3. [중거리] 돌진 or 투척
        if (dist > attackRange && dist <= 15.0f)
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

        // 아무것도 못 함 (쿨타임 중)
        return false;
    }

    // 약점 피격 시 호출
    public void OnCoreHit()
    {
        float bonusDamage = stats.MaxHP * 0.1f;
        TakeDamage(bonusDamage);

        // 스턴(Overload) 상태로 전환
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
        float timer = 0f;
        float sinkTime = 3.0f;

        while (timer < sinkTime)
        {
            transform.Translate(Vector3.down * 0.5f * Time.deltaTime, Space.World);
            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    // 설정한 구체 내에 있는 플레이어에게 데미지를 주도록 변경
    public void PerformBasicAttack()
    {

        Vector3 attackPos = transform.position + (transform.forward * 1.5f) + (Vector3.down * 0.5f);

        // 공격 반경 (Radius)
        float attackRadius = 5.0f;

        // ----------------------------------------------------

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

        if (hitSomething) Debug.Log("<color=green>평타 적중!</color>");
        else Debug.Log($"<color=red>평타 빗나감 (판정 범위 확인 필요)</color>");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        // PerformBasicAttack과 동일한 수치로 맞춰야 확인가능
        Vector3 attackPos = transform.position + (transform.forward * 1.5f) + (Vector3.down * 0.5f);
        Gizmos.DrawWireSphere(attackPos, 5.0f);
    }
}