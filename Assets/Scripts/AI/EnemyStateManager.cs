using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyStateManager : MonoBehaviour, IDamageable
{
    [HideInInspector] public IdleState idleState = new IdleState();
    [HideInInspector] public ChaseState chaseState = new ChaseState();
    // [삭제] PatrolState 제거
    [HideInInspector] public DeadState deadState = new DeadState();

    public IEnemyState attackState;
    public IEnemyState currentState;

    public EnemyData stats;             // 적의 스탯 정보

    // [삭제] patrolPoints 배열 삭제 (더 이상 필요 없음 -> 오류 해결)
    public Transform firePoint;

    public float currentHP;
    public float distanceToPlayer;
    public float attackTimer;           // 공격 쿨타임 관리용
    // [삭제] currentPatrolIndex 삭제

    [HideInInspector] public NavMeshAgent navMeshAgent;
    [HideInInspector] public Animator animator;
    [HideInInspector] public Transform playerTransform;

    private MeshRenderer meshRenderer;
    private Material originalMaterial;  // 원래 재질 저장용

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
            originalMaterial = meshRenderer.material;
    }

    void Start()
    {
        if (stats != null)
        {
            navMeshAgent.speed = stats.MoveSpeed;
            currentHP = stats.MaxHP;
            SetupAttackState();
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            // Debug.Log("플레이어 찾기 성공! 대상: " + playerObj.name);
        }

        // [삭제] 패트롤 포인트 태그 검색 로직 삭제

        TransitionToState(idleState);
    }

    void Update()
    {
        if (playerTransform != null)    // 플레이어와의 거리 계산
            distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        else
            distanceToPlayer = Mathf.Infinity;

        currentState?.UpdateState(this);

        if (attackTimer > 0) attackTimer -= Time.deltaTime;
    }

    public void TransitionToState(IEnemyState newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    void SetupAttackState() // 공격 타입 설정
    {
        if (stats.CombatType == EnemyCombatType.Ranged)
            attackState = new RangeAttackState();
        else
            attackState = new AttackState();
    }

    public void OnAttackAnimationFinished()
    {
        TransitionToState(chaseState);
    }

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

    public void TriggerHitVisual() // 피격 효과
    {
        Material hitMat = Resources.Load<Material>("HitMaterial");
        if (hitMat != null) StartCoroutine(HitFlashCoroutine(hitMat));
    }

    private IEnumerator HitFlashCoroutine(Material hitMat)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material = hitMat;
            yield return new WaitForSeconds(0.2f);
            meshRenderer.material = originalMaterial;
        }
    }

    public void StartDeathSequence() // 사망 처리
    {
        StartCoroutine(DeathCoroutine());
    }

    private IEnumerator DeathCoroutine()
    {
        float timer = 0f;
        float sinkTime = 3.0f;

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, 0, 90);

        while (timer < sinkTime)
        {
            transform.Translate(Vector3.down * 0.5f * Time.deltaTime, Space.World);
            float t = timer / sinkTime;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    // [수정] 풀링 초기화 함수 (소환될 때 호출됨)
    public void ResetEnemy()
    {
        currentHP = stats.MaxHP;
        navMeshAgent.enabled = true;
        navMeshAgent.isStopped = true; // 일단 멈춘 상태로 시작 (플레이어 인식 전까지)
        GetComponent<Collider>().enabled = true;

        // 다시 Idle 상태로 시작
        TransitionToState(idleState);
    }


    void GiveExpToPlayer()
    {
        if (playerTransform == null) return;

        // 이후 실제 플레이어 스크립트로 교체
        var playerScript = playerTransform.GetComponent<SimplePlayerMover>();

        if (playerScript != null)
        {
            playerScript.AddExp(stats.DropExp);
        }
    }



}