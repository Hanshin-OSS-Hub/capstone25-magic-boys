using UnityEngine;

public class BossAnimationRelay : MonoBehaviour
{
    [SerializeField] private BossStateManager bossManager;

    void Awake()
    {
        if (bossManager == null)
        {
            bossManager = GetComponentInParent<BossStateManager>();
        }
    }

    //  공격 종료 신호 
    public void OnBossAttackFinished()
    {
        if (bossManager != null) bossManager.OnBossAttackFinished();
    }

    //  평타 공격 판정 신호
    public void PerformBasicAttack()
    {
        if (bossManager != null)
        {
            bossManager.PerformBasicAttack();
        }
    }
}