using System;
using UnityEngine;

public class RangeAttackState : IEnemyState
{
    private float attackAnimationTimer; // 애니메이션이 없는 현재, 임시로 쓸 공격 애니메이션 타이머

    public void EnterState(EnemyStateManager enemy)
    {
        Debug.Log("원거리 공격 시작!");
        enemy.navMeshAgent.isStopped = true;
        enemy.transform.LookAt(enemy.playerTransform);

        enemy.attackTimer = enemy.stats.AttackCooldown;

        attackAnimationTimer = 1.0f; // 임시: 1초 동안 공격 애니메이션 재생
    }

    public void ExitState(EnemyStateManager enemy)
    {
        Debug.Log("원거리 공격 종료.");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        enemy.transform.LookAt(enemy.playerTransform);

        attackAnimationTimer -= Time.deltaTime;
        if (attackAnimationTimer <= 0)
        {
            PerformRangedAttack(enemy);
            enemy.OnAttackAnimationFinished(); // 애니메이션 종료 후 ChaseState로 전환
        }
    }

    private void PerformRangedAttack(EnemyStateManager enemy) // 공격 수행 함수
    {
        if (enemy.stats.projectilePrefab != null && enemy.firePoint != null)
        {
            GameObject bullet = GameObject.Instantiate(enemy.stats.projectilePrefab, enemy.firePoint.position, enemy.firePoint.rotation);

            // (선택사항) 투사체 데미지 설정
            EnemyProjectile proj = bullet.GetComponent<EnemyProjectile>();
            if (proj != null) proj.damage = enemy.stats.Damage;
        }
    }
}