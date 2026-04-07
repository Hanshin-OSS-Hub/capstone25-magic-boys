using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("소환할 보스 프리팹을 연결하세요.")]
    public GameObject bossPrefab;

    [Tooltip("보스가 나타날 위치 오브젝트를 연결하세요.")]
    public Transform spawnPoint;

    // 중복 소환을 막기 위한 플래그
    private bool hasSpawned = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasSpawned)
        {
            SpawnBoss();
        }
    }

    private void SpawnBoss()
    {
        hasSpawned = true;

        if (bossPrefab != null && spawnPoint != null)
        {
            Instantiate(bossPrefab, spawnPoint.position, spawnPoint.rotation);
            Debug.Log("보스 소환 완료!");

            // 소환 트리거 비활성화
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("인스펙터 창에서 보스 프리팹이나 스폰 위치가 할당되지 않았습니다!");
        }
    }
}