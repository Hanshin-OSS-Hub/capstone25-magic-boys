using StarterAssets;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using StarterAssets;

public class BossRushState : IBossState
{
    private float timer;
    private bool isRushing; // 현재 돌진 중인가?
    private Vector3 rushTargetPos; // 돌진 목표 지점 (고정됨)
    private GolemData data;

    // 돌진 중 충돌 처리를 위한 변수
    private bool hasHitPlayer;

    public void EnterState(BossStateManager boss)
    {
        Debug.Log("보스: 돌진 준비 (1초 대기)");

        boss.animator.SetBool("isRushing", true);

        data = boss.stats as GolemData;
        float chargeTime = (data != null) ? data.RushChargeTime : 1.0f;

        timer = chargeTime;
        isRushing = false;
        hasHitPlayer = false;

        boss.navMeshAgent.isStopped = true;
        boss.navMeshAgent.angularSpeed = 2000f; // 플레이어 조준을 위한 회전 속도 증가
    }

    public void UpdateState(BossStateManager boss)
    {
        // === 페이즈 1: 차징 (조준) ===
        if (!isRushing)
        {
            timer -= Time.deltaTime;

            // 플레이어 바라보기
            if (boss.playerTransform != null)
            {
                Vector3 targetPos = new Vector3(
                    boss.playerTransform.position.x,
                    boss.transform.position.y,
                    boss.playerTransform.position.z
                );

                boss.transform.LookAt(targetPos);
            }

            // 차징 끝 -> 돌진 시작!
            if (timer <= 0)
            {
                StartRush(boss);
            }
        }
        // === 페이즈 2: 돌진 (이동) ===
        else
        {
            // 1. 플레이어 충돌 판정 (직접 계산)
            CheckCollision(boss);

            // 2. 목표 지점 도착 확인
            // (NavMeshAgent가 목표에 거의 도착했거나, 더 이상 갈 수 없을 때)
            if (!boss.navMeshAgent.pathPending)
            {
                if (boss.navMeshAgent.remainingDistance <= boss.navMeshAgent.stoppingDistance + 0.5f)
                {
                    // 도착!
                    boss.TransitionToState(boss.chaseState);
                }
            }
        }
    }

    public void ExitState(BossStateManager boss)
    {
        boss.animator.SetBool("isRushing", false);
        // 상태 복구 (속도 등 원상복구)
        boss.navMeshAgent.isStopped = false;
        boss.navMeshAgent.speed = boss.stats.MoveSpeed; // 원래 속도로
        boss.navMeshAgent.angularSpeed = 120f; // 원래 회전 속도로
        boss.navMeshAgent.autoBraking = true;  // 브레이크 다시 켜기
        boss.rushCooldownTimer = data.RushCooldown;   // 돌진 쿨타임 초기화
    }

    private void StartRush(BossStateManager boss)
    {
        Debug.Log("보스: 돌진!!!!");
        isRushing = true;

        if (data == null || boss.playerTransform == null) return;

        // 목표 지점 확정 (현재 플레이어 위치 + 뒤로 조금 더)
        // (플레이어 위치 딱 거기까지만 가면 멈추니까, 좀 더 뒤쪽으로 목표를 잡아서 뚫고 지나가게 함)
        Vector3 direction = (boss.playerTransform.position - boss.transform.position).normalized;
        rushTargetPos = boss.playerTransform.position + (direction * 5.0f);

        boss.navMeshAgent.isStopped = false;
        boss.navMeshAgent.speed = data.RushSpeed; // 돌진 속도 적용
        boss.navMeshAgent.acceleration = 100f;    // 가속도 최대
        boss.navMeshAgent.angularSpeed = 0f;      // 돌진 중에는 회전 불가 (직진)
        boss.navMeshAgent.autoBraking = false;    // 목표 지점에서 감속하지 않음 (그냥 들이박음)

        // 이동 명령
        boss.navMeshAgent.SetDestination(rushTargetPos);
    }
    private void CheckCollision(BossStateManager boss)
    {
        if (hasHitPlayer || boss.playerTransform == null) return;

        // 거리 계산 대신 보스 앞쪽에 가상의 구(Hitbox)를 생성
        Vector3 checkPosition = boss.transform.position + (boss.transform.forward * 1.5f) + (Vector3.up * 1.0f);

        float hitRadius = 2.0f;

        Collider[] hitColliders = Physics.OverlapSphere(checkPosition, hitRadius);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                hasHitPlayer = true;

                // 1. 역경직 (타격감 보강)
                boss.navMeshAgent.speed = 2.0f;

                // 2. 데미지 처리
                IDamageable target = hitCollider.GetComponent<IDamageable>();
                if (target != null)
                {
                    Debug.Log("Rush 패턴 적중");
                    target.TakeDamage(data.RushDamage);
                }

                if (boss.rushSound != null)
                {
                    SoundManager.Instance.PlaySFX3D(boss.rushSound, boss.transform.position);
                }

                // 3. 넉백 처리
                ThirdPersonController playerController = hitCollider.GetComponent<ThirdPersonController>();
                if (playerController != null)
                {
                    Vector3 dirToPlayer = (boss.playerTransform.position - boss.transform.position).normalized;
                    float dot = Vector3.Dot(boss.transform.right, dirToPlayer);

                    Vector3 pushDir = (dot > 0) ? boss.transform.right : -boss.transform.right;

                    // 앞쪽 밀어내기 비중 축소 (보스와 겹침 방지)
                    pushDir = (pushDir + boss.transform.forward * 0.2f).normalized;
                    pushDir.y = 0;

                    // 수평/수직 분리 적용
                    Vector3 horizontalVelocity = pushDir * data.RushKnockback;
                    Vector3 verticalVelocity = Vector3.up * 12f;

                    Vector3 finalKnockbackVelocity = horizontalVelocity + verticalVelocity;

                    playerController.TakeKnockback(finalKnockbackVelocity, 0.7f);

                    Debug.Log(dot > 0 ? "오른쪽 대각선으로 쳐냄!" : "왼쪽 대각선으로 쳐냄!");
                }
                break; 
            }
        }
    }
}
