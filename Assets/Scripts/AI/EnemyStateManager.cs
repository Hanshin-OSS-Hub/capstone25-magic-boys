using System;
using System.Xml;
using UnityEngine;
using UnityEngine.AI; // NavMeshAgent 사용을 위해 필요
using System.Collections; // 코루틴 사용을 위해 필요

public class EnemyStateManager : MonoBehaviour, IDamageable
{
    [HideInInspector] public IdleState idleState = new IdleState();
    [HideInInspector] public ChaseState chaseState = new ChaseState();
    [HideInInspector] public AttackState attackState = new AttackState();
    [HideInInspector] public PatrolState patrolState = new PatrolState();
    [HideInInspector] public DeadState deadState = new DeadState();

    [SerializeField] EnemyData stats; // 적의 스탯 정보
    [SerializeField] Transform[] patrolPoints; // 순찰 지점  |  *주의* unity 6000.0.58f1에서 인스펙터창에 배열을 사용하게 되면 심각한 버그가 생김, Preferences -> General -> Editor Font: Inter에서 System Font로 변경하여 해결
    [SerializeField] Material hitMaterial; // 피격 시 변경할 재질

    public IEnemyState CurrentState;    // 현재 상태를 담는 변수
    public float attackTimer;           // 공격 쿨타임 관리용
    public int currentPatrolIndex = 0;  // 현재 순찰 지점 인덱스
    public float currentHP;
    private MeshRenderer meshRenderer;
    private Material originalMaterial; // 원래 재질 저장용

    public float DistanceToPlayer { get; private set; } // Manager만 쓰기가능, 나머지에선 읽기전용으로 선언된 변수
    private NavMeshAgent navMeshAgent; // 경로탐색 에이전트
    private Transform playerTransform; // 플레이어 위치정보

    public NavMeshAgent NavMeshAgent => navMeshAgent; // => : 읽기 전용, navMeshAgent, playerTransform = null; 설정방지
    public Transform PlayerTransform => playerTransform;




    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponent<MeshRenderer>();
        originalMaterial = meshRenderer.material;
    }

    void Start()
    {
        if (stats != null)
        {
            navMeshAgent.speed = stats.MoveSpeed;
            currentHP = stats.MaxHP;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            Debug.Log("플레이어 찾기 성공! 대상: " + playerObject.name);
        }
        else
        {
            Debug.LogError("플레이어를 찾지 못함");
        }
        TransitionToState(idleState);
    }

    void Update()
    {
        CalculateCommonData();
        CurrentState?.UpdateState(this); // ?: null이 아닐 때만 실행
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
    }

    public void TransitionToState(IEnemyState newState) //상태 전환
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState.EnterState(this);
        print($"상태전환: {newState}");
    }

    public void OnAttackAnimationFinished() { TransitionToState(chaseState); } // 공격 애니메이션 종료 시 추격 상태로 전환
    public float GetDetectionRange() { return stats.DetectionRange; }
    public float GetAttackRange() { return stats.AttackRange; }
    public float GetAttackCooldown() { return stats.AttackCooldown; }
    public float GetDamage() { return stats.Damage; }
    public Transform[] GetPatrolPoints() { return patrolPoints; }


    void CalculateCommonData()
    {
        if (PlayerTransform != null) //플레이어와의 거리 계산
        {
            DistanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
        }
        else DistanceToPlayer = Mathf.Infinity;
    }

    public void TakeDamage(float damage)
    {
        if (currentHP <= 0) return; // 이미 죽은 상태면 무시

        TriggerHitVisual(); 
        currentHP -= damage;

        if (currentHP <= 0)
        {
            TransitionToState(deadState);
        }
      
    }

    private void TriggerHitVisual() // 피격 효과 실행
    {
        StartCoroutine(HitFlashCoroutine());
    }

    private IEnumerator HitFlashCoroutine() // 피격 효과
    {
        meshRenderer.material = hitMaterial;
        yield return new WaitForSeconds(0.2f);
        meshRenderer.material = originalMaterial;
    }

    public void StartDeathSequence() // 죽음 처리 코루틴
    {
        StartCoroutine(DeathCoroutine(1.5f));
    }


    private IEnumerator DeathCoroutine(float sinkTime)
    {
        float timer = 0f;
        float sinkSpeed = 0.5f;

        Quaternion startRotation = transform.rotation; // 현재 회전값 저장
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, 0, 90); // 현재 값에서 Z축으로 90도(왼쪽) 회전한 목표 설정

        while (timer < sinkTime)
        {
            transform.Translate(Vector3.down * sinkSpeed * Time.deltaTime, Space.World);
           
            float t = timer / sinkTime; // 현재 진행률 계산 (0.0에서 1.0까지)

            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t); // 회전 모션 보간

            timer += Time.deltaTime;
            yield return null;

        }

        Destroy(gameObject);
    }


}
