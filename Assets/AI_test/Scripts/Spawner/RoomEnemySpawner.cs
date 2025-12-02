using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.AI;

public class RoomEnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public EnemyCombatType enemyType;
    public int spawnCount = 3;

    public List<Transform> spawnPoints;

    // [삭제] roomPatrolPoints 변수 삭제

    private List<GameObject> activeEnemies = new List<GameObject>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) SpawnEnemies();
        if (Input.GetKeyDown(KeyCode.L)) DespawnEnemies();
    }

    public void SpawnEnemies()
    {
        if (spawnPoints == null || spawnPoints.Count == 0) return;
        if (activeEnemies.Count > 0) return;

        // 1. 중복 방지를 위한 리스트 복사
        List<Transform> availablePoints = new List<Transform>(spawnPoints);
        int countToSpawn = Mathf.Min(spawnCount, availablePoints.Count);

        for (int i = 0; i < countToSpawn; i++)
        {
            // 2. [여기!] point 변수 정의 (카드 뽑기)
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform point = availablePoints[randomIndex]; // 여기서 'point'가 만들어집니다.
            availablePoints.RemoveAt(randomIndex); // 뽑은 자리는 리스트에서 제거

            // 3. 적을 가져옴 (아직 꺼져있음!)
            GameObject enemy = ObjectPool.Instance.GetEnemy(enemyType);

            // 4. 위치 먼저 이동 (워프) - 이제 'point'를 쓸 수 있습니다.
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();

            // 꺼져있을 땐 Warp가 안 먹힐 수 있으니 position으로 강제 이동
            enemy.transform.position = point.position;
            enemy.transform.rotation = point.rotation;

            // 5. 이제 안전한 위치에 왔으니 켭니다! (에러 안 남)
            enemy.SetActive(true);

            // 6. 초기화
            EnemyStateManager enemyState = enemy.GetComponent<EnemyStateManager>();
            enemyState.ResetEnemy();

            // 7. (확인 사살) 켜진 뒤에 혹시 모르니 한 번 더 Warp
            if (agent != null && agent.isOnNavMesh) agent.Warp(point.position);

            activeEnemies.Add(enemy);
        }
    }

    public void DespawnEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy.activeSelf)
                ObjectPool.Instance.ReturnEnemy(enemy, enemyType);
        }
        activeEnemies.Clear();
    }
}