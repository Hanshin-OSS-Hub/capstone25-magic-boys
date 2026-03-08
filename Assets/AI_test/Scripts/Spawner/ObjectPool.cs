using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance;

    [Header("Pool Settings")]
    public GameObject meleeEnemyPrefab;   // 근접 적 프리팹
    public GameObject rangedEnemyPrefab;  // 원거리 적 프리팹
    public int poolSize = 20;             // 종류별로 미리 만들 개수

    // 대기 중인 적들을 담아둘 큐 (Queue)
    private Queue<GameObject> meleePool = new Queue<GameObject>();
    private Queue<GameObject> rangedPool = new Queue<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializePool(meleeEnemyPrefab, meleePool);
        InitializePool(rangedEnemyPrefab, rangedPool);
    }

    // 풀 초기화 (미리 생성해서 비활성화해둠)
    void InitializePool(GameObject prefab, Queue<GameObject> pool)
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform); // 정리하기 쉽게 풀 오브젝트 자식으로
            pool.Enqueue(obj);
        }
    }

    // 적을 꺼내오는 함수
    public GameObject GetEnemy(EnemyCombatType type)
    {
        Queue<GameObject> targetPool = (type == EnemyCombatType.Melee) ? meleePool : rangedPool;

        if (targetPool.Count > 0)
        {
            GameObject obj = targetPool.Dequeue();
            return obj;
        }
        else
        {
            // 풀이 모자라면 새로 만들어서 줌 (비상용)
            GameObject prefab = (type == EnemyCombatType.Melee) ? meleeEnemyPrefab : rangedEnemyPrefab;
            GameObject newObj = Instantiate(prefab);
            return newObj;
        }
    }

    // 적을 반납하는 함수 (죽거나 방을 나갈 때)
    public void ReturnEnemy(GameObject enemy, EnemyCombatType type)
    {
        enemy.SetActive(false);
        enemy.transform.SetParent(transform); // 다시 풀 아래로 정리

        if (type == EnemyCombatType.Melee) meleePool.Enqueue(enemy);
        else rangedPool.Enqueue(enemy);
    }
}