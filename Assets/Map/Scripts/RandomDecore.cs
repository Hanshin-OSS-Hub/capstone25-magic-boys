using UnityEngine;

public class RandomDecore : MonoBehaviour
{
    [SerializeField]
    GameObject[] decorPrefabs;

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
            GameObject goDecor = Instantiate(decorPrefabs[decorIndex], transform.position,transform.rotation, transform) as GameObject;
            goDecor.name = decorPrefabs[decorIndex].name;
        }
    }
}
