using UnityEngine;
using UnityEngine.AI; // NavMeshAgent 사용을 위해 필요

public class EnemyStateManager : MonoBehaviour
{
    // 상태 인스턴스 생성
    [HideInInspector] public IdleState idleState = new IdleState();
    [HideInInspector] public ChaseState chaseState = new ChaseState();
    [HideInInspector] public AttackState attackState = new AttackState();
    [HideInInspector] public PatrolState patrolState = new PatrolState();

    [SerializeField] EnemyData stats; // 적의 스탯 정보
    // *주의* unity 6000.0.58f1에서 인스펙터창에 배열을 사용하게 되면 심각한 버그가 생김
    // Preferences -> General -> Editor Font: Inter에서 System Font로 변경하여 해결
    [SerializeField] Transform[] patrolPoints; // 순찰 지점

    public IEnemyState CurrentState; // 현재 상태를 담는 변수
    public float attackTimer; // 공격 쿨타임 관리용
    public int currentPatrolIndex = 0; // 현재 순찰 지점 인덱스

    public float DistanceToPlayer { get; private set; } // Manager만 쓰기가능, 나머지에선 읽기전용으로 선언된 변수
    private NavMeshAgent navMeshAgent; // 경로탐색 에이전트
    private Transform playerTransform; // 플레이어 위치정보


    
    // => : 읽기 전용
    // navMeshAgent, playerTransform = null; 설정방지
    public NavMeshAgent NavMeshAgent => navMeshAgent;
    public Transform PlayerTransform => playerTransform;





    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();        
    }

    void Start()
    {

        if (stats != null)
        {
            navMeshAgent.speed = stats.MoveSpeed;
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

    public void OnAttackAnimationFinished() { TransitionToState(chaseState); }

    public float GetDetectionRange() { return stats.DetectionRange; }
    public float GetAttackRange() { return stats.AttackRange; }
    public float GetAttackCooldown() { return stats.AttackCooldown; }
    public Transform[] GetPatrolPoints() { return patrolPoints; }


    void CalculateCommonData()
    {
        if (PlayerTransform != null) //플레이어와의 거리 계산
        {
            DistanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
        }
        else DistanceToPlayer = Mathf.Infinity;
    }

    


}
