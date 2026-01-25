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

    private List<GameObject> activeEnemies = new List<GameObject>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) SpawnEnemies();
        if (Input.GetKeyDown(KeyCode.O)) DespawnEnemies();
    }

    public void SpawnEnemies()
    {
        if (spawnPoints == null || spawnPoints.Count == 0) return;
        if (activeEnemies.Count > 0) return; // 이미 소환했으면 패스

        // 2. 중복 방지를 위한 리스트 복사 (카드 덱 만들기)
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        // 소환할 숫자 결정 (설정된 수 vs 남은 자리 수 중 작은 거)
        int countToSpawn = Mathf.Min(spawnCount, availablePoints.Count);

        for (int i = 0; i < countToSpawn; i++)
        {
            // 3. 랜덤 자리 뽑기 (카드 뽑기)
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform rawPoint = availablePoints[randomIndex]; // 원본 스폰 포인트
            availablePoints.RemoveAt(randomIndex); // 뽑은 자리는 리스트에서 제거 (중복 방지)

            // 4. 풀에서 적 가져오기 (아직 꺼진 상태)
            GameObject enemy = ObjectPool.Instance.GetEnemy(enemyType);

            // 5.  바닥 높이 보정 로직 (Raycast) 
            Vector3 spawnPos = rawPoint.position;
            RaycastHit hit;

            // 스폰 포인트 4m 위에서 아래로 레이저를 쏴서 바닥을 찾음
            if (Physics.Raycast(rawPoint.position + Vector3.up * 4.0f, Vector3.down, out hit, 5.0f))
            {
                // 바닥(Collider/NavMesh)을 찾았으면 그 위로 위치 보정 0.2정도 띄움
                spawnPos = hit.point + Vector3.up * 0.2f;
            }
            else
            {
                // 바닥을 못 찾았으면 안전하게 원래 위치보다 살짝 위에 소환
                spawnPos = rawPoint.position + Vector3.up * 0.5f;
            }

            // 6. 위치 먼저 이동
            // (NavMeshAgent가 켜지기 전에 transform을 옮겨야 에러가 안 남)
            enemy.transform.position = spawnPos;
            enemy.transform.rotation = rawPoint.rotation;

            enemy.SetActive(true);

            // 8. 상태 초기화 (HP 회복, Idle 상태로 등)
            EnemyStateManager enemyState = enemy.GetComponent<EnemyStateManager>();
            if (enemyState != null) enemyState.ResetEnemy();

            // 9. (확인 사살) NavMeshAgent 워프
            // 켜진 직후 NavMesh 위에 확실히 안착시키기 위해 Warp 한 번 더 수행
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(spawnPos);
            }

            // 10. 관리 리스트에 추가
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