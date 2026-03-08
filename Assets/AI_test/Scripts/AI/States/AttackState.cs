using System;
using UnityEngine;

public class AttackState : IEnemyState
{
    private float attackAnimationTimer; // 애니메이션이 없는 현재, 임시로 쓸 공격 애니메이션 타이머

    public void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("공격 시작!");
        enemy.navMeshAgent.isStopped = true;
        enemy.transform.LookAt(enemy.playerTransform);
        
        enemy.attackTimer = enemy.stats.AttackCooldown;

        attackAnimationTimer = 1.0f; // 임시: 1초 동안 공격 애니메이션 재생
    }

    public void ExitState(EnemyStateManager enemy)
    {
        Debug.Log("공격 종료.");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        // 플레이어를 계속 바라봄
        enemy.transform.LookAt(enemy.playerTransform);

        attackAnimationTimer -= Time.deltaTime;
        if (attackAnimationTimer <= 0) 
        {
            PerformAttack(enemy);
            enemy.OnAttackAnimationFinished(); 
        }
    }

    private void PerformAttack(EnemyStateManager enemy) // 공격 수행 함수
    {
        if (enemy.playerTransform == null) return;
        IDamageable player = enemy.playerTransform.GetComponent<IDamageable>();
        if (player != null)
        {
            if (enemy.distanceToPlayer <= enemy.stats.AttackRange) 
            {
                player.TakeDamage(enemy.stats.Damage);
            }
        }
    }
}