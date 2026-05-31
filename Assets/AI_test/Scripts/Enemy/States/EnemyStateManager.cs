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

    public AudioClip attackSound;
    public AudioClip deadSound;

    private Renderer[] renderers;
    private Material[][] originalMaterials;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
        }
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

    // 애니메이션 이벤트가 호출할 공격 판정 함수
    public void PerformAttack()
    {
        if (playerTransform == null) return;
        if (attackSound != null)
        {
            SoundManager.Instance.PlaySFX3D(attackSound, transform.position);
        }

        // 공격 사거리 체크
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= stats.AttackRange)
        {
            IDamageable target = playerTransform.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(stats.Damage);
                //Debug.Log("공격 적중!");
            }

        }
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
        else
        {
            if (playerTransform != null && currentState == idleState)
            {
                TransitionToState(chaseState);
            }
        }
    }

    public void TriggerHitVisual()
    {
        var hitMat = Resources.Load<Material>("HitMaterial");
        if (hitMat != null) StartCoroutine(HitFlashCoroutine(hitMat));
    }

    IEnumerator HitFlashCoroutine(Material hitMat)
    {
        if (renderers == null) yield break;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material[] hitMats = new Material[renderers[i].materials.Length];
            for (int j = 0; j < hitMats.Length; j++) hitMats[j] = hitMat;
            renderers[i].materials = hitMats;
        }

        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].materials = originalMaterials[i];
        }
    }

    public void StartDeathSequence()
    {
        if (deadSound != null)
        {
            SoundManager.Instance.PlaySFX3D(deadSound, transform.position);
        }
        StartCoroutine(DeathCoroutine());
    }

    IEnumerator DeathCoroutine()
    {
        // (주석코드)사망 애니메이션이 없을 경우 가라앉는 연출로 대체
        //float timer = 0f;
        //float sinkTime = 3f;
        //Quaternion startRot = transform.rotation;
        //Quaternion targetRot = startRot * Quaternion.Euler(0, 0, 90);

        //while (timer < sinkTime)
        //{
        //    transform.Translate(Vector3.down * 0.5f * Time.deltaTime, Space.World);
        //    transform.rotation = Quaternion.Slerp(startRot, targetRot, timer / sinkTime);
        //    timer += Time.deltaTime;
        //    yield return null;
        //}

        yield return new WaitForSeconds(2.0f);
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

    // EXP 지급: PlayerStats로 변경 (SimplePlayerMover → PlayerStats)
    void GiveExpToPlayer()
    {
        if (!playerTransform) return;
        //var ps = playerTransform.GetComponent<PlayerStats>();
        //if (ps != null) ps.AddExp(stats.DropExp);
    }
}