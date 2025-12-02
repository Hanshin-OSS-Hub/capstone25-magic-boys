public interface IBossState
{
    void EnterState(BossStateManager boss);
    void UpdateState(BossStateManager boss);
    void ExitState(BossStateManager boss);
}