using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("적 설정")]
    public GameObject[] enemyPrefabs;
    public int minEnemiesPerPoint = 3;
    public int maxEnemiesPerPoint = 4;
    public float spawnRadius = 5.0f;

    private void OnEnable()
    {
        DungeonGenerator.OnMapCompleted += HandleMapCompleted;
    }

    private void OnDisable()
    {
        DungeonGenerator.OnMapCompleted -= HandleMapCompleted;
    }

    private void HandleMapCompleted()
    {
        Debug.Log("SpawnEnemies...");
        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawnPoint");
        if (spawnPoints.Length == 0) return;

        GameObject enemyParentObj = GameObject.Find("Enemy");
        if (enemyParentObj == null)
        {
            Debug.LogError("하이어라키에 'Enemy' 오브젝트가 없습니다! 미리 생성해주세요.");
            return;
        }
        Transform enemyParent = enemyParentObj.transform;

        int totalSpawned = 0;

        foreach (GameObject point in spawnPoints)
        {
            int spawnCount = Random.Range(minEnemiesPerPoint, maxEnemiesPerPoint + 1);

            for (int i = 0; i < spawnCount; i++)
            {
                int enemyIndex = Random.Range(0, enemyPrefabs.Length);
                GameObject selectedEnemy = enemyPrefabs[enemyIndex];

                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 randomPos = point.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPos, out hit, 2.0f, NavMesh.AllAreas))
                {
                    Instantiate(selectedEnemy, hit.position, point.transform.rotation, enemyParent);
                    totalSpawned++;
                }
            }
        }
        Debug.Log($"{totalSpawned}개의 적 개체 생성 완료.");
    }
}