using System;
using UnityEngine;

public class AttackState : IEnemyState
{
    private float attackAnimationTimer; // 애니메이션이 없는 현재, 임시로 쓸 공격 애니메이션 타이머

    public void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("공격 시작!");
        enemy.NavMeshAgent.isStopped = true;
        enemy.transform.LookAt(enemy.PlayerTransform);
        enemy.attackTimer = enemy.GetAttackCooldown();

        attackAnimationTimer = 1.0f; // 임시: 1초 동안 공격 애니메이션 재생
    }

    public void ExitState(EnemyStateManager enemy)
    {
        Debug.Log("공격 종료.");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        enemy.transform.LookAt(enemy.PlayerTransform);

        attackAnimationTimer -= Time.deltaTime;
        if (attackAnimationTimer <= 0) 
        {
            PerformAttack(enemy);
            enemy.OnAttackAnimationFinished(); 
        }
    }

    private void PerformAttack(EnemyStateManager enemy) // 공격 수행 함수
    {
        if (enemy.PlayerTransform == null) return;
        IDamageable player = enemy.PlayerTransform.GetComponent<IDamageable>();
        if (player != null)
        {
            if (enemy.DistanceToPlayer <= enemy.GetAttackRange()) // 애니메이션이 끝났을 때 공격 범위 내에 있다면 데미지줌
            {
                player.TakeDamage(enemy.GetDamage());
            }
        }

    }
}
