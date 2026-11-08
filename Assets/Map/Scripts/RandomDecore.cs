using UnityEngine;

public class RandomDecore : MonoBehaviour
{
    [SerializeField]
    GameObject[] decorPrefabs;

    // 방(타일)이 1.4배로 확대되면서 이 오브젝트 아래 생성되는 소품도 같이 커져 플레이어 대비 너무 컸음.
    // 부모 스케일을 상쇄한 뒤 이 배율을 월드 기준으로 적용한다. (1 = 소품 원본 크기)
    [SerializeField, Range(0.2f, 1.5f)]
    float decorWorldScale = 0.75f;

    DungeonGenerator myGenerator;
    bool isCompleted = false;

    void Start()
    {
        myGenerator = GameObject.Find("Generator").GetComponent<DungeonGenerator>();
    }

    void Update()
    {
        if (!isCompleted && myGenerator.dungeonState == DungeonGenState.completed)
        {
            isCompleted = true;
            int decorIndex = Random.Range(0, decorPrefabs.Length);
            GameObject prefab = decorPrefabs[decorIndex];
            GameObject goDecor = Instantiate(prefab, transform.position, transform.rotation, transform) as GameObject;
            goDecor.name = prefab.name;

            float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);
            goDecor.transform.localScale = prefab.transform.localScale * (decorWorldScale / parentScale);
        }
    }
}
