using System;
using UnityEngine;

public class RangeAttackState : IEnemyState
{
    private float attackAnimationTimer; // 애니메이션이 없는 현재, 임시로 쓸 공격 애니메이션 타이머

    public void EnterState(EnemyStateManager enemy)
    {
        //Debug.Log("원거리 공격 시작");
        enemy.navMeshAgent.isStopped = true;

        Vector3 targetPos = enemy.playerTransform.position;
        targetPos.y += 1.0f;

        enemy.transform.LookAt(targetPos);

        enemy.attackTimer = enemy.stats.AttackCooldown;

        attackAnimationTimer = 1.0f; // 임시: 1초 동안 공격 애니메이션 재생
    }

    public void ExitState(EnemyStateManager enemy)
    {
        //Debug.Log("원거리 공격 종료.");
    }

    public void UpdateState(EnemyStateManager enemy)
    {
        Vector3 targetPos = enemy.playerTransform.position;
        targetPos.y += 1.0f;
        enemy.transform.LookAt(targetPos);

        attackAnimationTimer -= Time.deltaTime;
        if (attackAnimationTimer <= 0)
        {
            PerformRangedAttack(enemy);
            enemy.OnAttackAnimationFinished(); // 애니메이션 종료 후 ChaseState로 전환
        }
    }

    private void PerformRangedAttack(EnemyStateManager enemy)
    {
        if (enemy.firePoint != null)
        {
            if (enemy.attackSound != null)
            {
                SoundManager.Instance.PlaySFX3D(enemy.attackSound, enemy.transform.position);
            }

            GameObject bullet = ProjectilePool.Instance.GetProjectile(enemy.firePoint.position, enemy.firePoint.rotation);

            EnemyProjectile proj = bullet.GetComponent<EnemyProjectile>();
            if (proj != null) proj.damage = enemy.stats.Damage;
        }
    }
}