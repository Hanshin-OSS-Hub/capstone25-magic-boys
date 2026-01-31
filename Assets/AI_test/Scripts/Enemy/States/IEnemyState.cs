public interface IEnemyState
{
    // 적 상태의 구현내용을 정하는 인터페이스
    void EnterState(EnemyStateManager enemy);
    void UpdateState(EnemyStateManager enemy);
    void ExitState(EnemyStateManager enemy);

}