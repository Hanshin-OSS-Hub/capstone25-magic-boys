using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Xml;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum DungeonGenState
{
    inactive, generatingMain, generatingBranches, cleanup, completed
}

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] GameObject[] tilePrefabs;
    [SerializeField] GameObject[] startPrefabs;
    [SerializeField] GameObject[] exitPrefabs;
    [SerializeField] GameObject[] blockedPrefabs;
    [SerializeField] GameObject[] doorPrefabs;
    [SerializeField] GameObject[] puzzlePrefabs;

    [Header("Debugging Options")]
    [SerializeField] bool useBoxColliders;
    [SerializeField] bool useLightsForDebugging;
    [SerializeField] bool restoreLightsAfterDebugging;


    [Header("Key Bindings")]
    [SerializeField] KeyCode reloadKey = KeyCode.Backspace;
    [SerializeField] KeyCode toggleMapKey = KeyCode.M;


    [Header("Generation Limits")]
    [UnityEngine.Range(2,100)][SerializeField] int mainLength = 10;
    [UnityEngine.Range(0, 50)][SerializeField] int branchLength = 5;
    [UnityEngine.Range(0, 25)][SerializeField] int numBranches = 10;
    [UnityEngine.Range(0, 100)][SerializeField] int doorPercent = 25;
    [UnityEngine.Range(0, 1f)][SerializeField] float constructionDelay;

    [Header("UI References")]
    [SerializeField] GameObject uiCanvas;
    [SerializeField] GameObject crosshairCanvas;

    [Header("Availabe at Runtime")]
    public List<Tile> genneratedTiles = new List<Tile>();

    [Header("Reload Map")]
    public string seneToLoad = "Game"; 

    [HideInInspector]
    public DungeonGenState dungeonState = DungeonGenState.inactive;

    public static event System.Action OnMapCompleted;

    GameObject goCamera, goPlayer;
    List<Connector> availableConnectors = new List<Connector>();
    Color startLightColor = Color.white;
    Transform tileFrom, tileTo, tileRoot;
    Transform container;
    int attempts;
    int maxAttempts = 50;

    public static int saveSeed = -1;

    void Start()
    {
        goCamera = GameObject.Find("OverheadCamera");       
        goPlayer = GameObject.FindWithTag("Player");
        if (uiCanvas == null) uiCanvas = GameObject.Find("Canvas(UI)");
        if (crosshairCanvas == null) crosshairCanvas = GameObject.Find("Canvas");
        if (saveSeed != -1)
        {
            Random.InitState(saveSeed);
        }
        else
        {
            saveSeed = Random.Range(0, int.MaxValue);
            Random.InitState(saveSeed);
        }

            StartCoroutine(DungeonBuild());
    }


    void ToggleMap(bool active)
    {
        if (goCamera != null) goCamera.SetActive(active);
        if (goPlayer != null) goPlayer.SetActive(!active);
        if (uiCanvas != null) uiCanvas.SetActive(!active);
        if (crosshairCanvas != null) crosshairCanvas.SetActive(!active);
    }

    private void Update()
    {
        if (Input.GetKeyDown(reloadKey))
        {
            saveSeed = -1;
            SceneManager.LoadScene(seneToLoad);
        }
        if(Input.GetKeyDown(toggleMapKey))
        {
            ToggleMap(!goCamera.activeInHierarchy);
        }
    }



    IEnumerator DungeonBuild()
    {
        ToggleMap(true);
        GameObject goContainer = new GameObject("Main Path");
        container = goContainer.transform;
        container.SetParent(transform);
        tileRoot = CreateStartTile();
        DebugRoomLighting(tileRoot, Color.blue);
        tileTo = tileRoot;
        dungeonState = DungeonGenState.generatingMain;
        while(genneratedTiles.Count < mainLength)
        {
            yield return new WaitForSeconds(constructionDelay);
            tileFrom = tileTo;
            if (genneratedTiles.Count == mainLength - 1)
            {
                //exit room
                tileTo = CreateExitTile();
                DebugRoomLighting(tileTo, Color.magenta);
            }
            else
            {
                tileTo = CreateTile();
                DebugRoomLighting(tileTo, Color.yellow);

            }
            ConnectTiles();
            CollisionCheck();
           
        }



        //get all connectors within container that not already connected
        foreach (Connector connector in container.GetComponentsInChildren<Connector>())
        {
            if(!connector.isConnected)
            {
                if(!availableConnectors.Contains(connector))
                {
                    availableConnectors.Add(connector);
                }
            }
        }
        //branching
        dungeonState = DungeonGenState.generatingBranches;
        for (int b = 0; b < numBranches; b++)
        {
            if (availableConnectors.Count > 0)
            {
                goContainer = new GameObject("Branch" + (b + 1));
                container = goContainer.transform;
                container.SetParent(transform);
                int availIndex = Random.Range(0, availableConnectors.Count);
                tileRoot = availableConnectors[availIndex].transform.parent.parent;
                availableConnectors.RemoveAt(availIndex);
                tileTo = tileRoot;
                for (int i = 0; i < branchLength - 1; i++)
                {
                    yield return new WaitForSeconds(constructionDelay);
                    tileFrom = tileTo;
                    tileTo = CreateTile();
                    DebugRoomLighting(tileTo, Color.green);
                    ConnectTiles();
                    CollisionCheck();
                    if (attempts >= maxAttempts) { break; }
                }
            }
            else { break; }
        }
        dungeonState = DungeonGenState.cleanup;
        yield return StartCoroutine(SpawnPuzzleRoom());
        LightRestoration();
        CleanupBoxes();
        BlockedPassages();
        SpawnDoors();
        dungeonState = DungeonGenState.completed;
        yield return null;
        ToggleMap(false);

        OnMapCompleted?.Invoke();
        }
void SpawnDoors()
    {
        if (doorPercent > 0)
        {
            Connector[] allConnectors = transform.GetComponentsInChildren<Connector>();
            for (int i = 0; i < allConnectors.Length; i++)
            {
                Connector myconnector = allConnectors[i];
                if (myconnector.isConnected)
                {
                    //random chance of spawning a door
                    int roll = Random.Range(1, 101);
                    if (roll <= doorPercent)
                    {
                        Vector3 halfExtents = new Vector3(myconnector.size.x, 1f, myconnector.size.x);
                        Vector3 pos = myconnector.transform.position;
                        Vector3 offset = myconnector.transform.up * 0.5f;
                        Collider[] hits = Physics.OverlapBox(pos + offset, halfExtents, Quaternion.identity, LayerMask.GetMask("Door"));
                        if (hits.Length == 0)
                        {
                            int doorIndex = Random.Range(0, doorPrefabs.Length);
                            GameObject goDoor = Instantiate(doorPrefabs[doorIndex], pos, myconnector.transform.rotation, myconnector.transform) as GameObject;
                            goDoor.name = doorPrefabs[doorIndex].name;
                            // 연결부 반대편 연결점에서 문 중복 검사가 바로 보이도록 물리 동기화 (문 2개 겹침 방지)
                            Physics.SyncTransforms();
                        }
                    }
                }
            }
        }
    }

    void BlockedPassages()
    {
        foreach(Connector connector in transform.GetComponentsInChildren<Connector>())
        {
            if (!connector.isConnected)
            {
                Vector3 pos = connector.transform.position;
                int walliIndex = Random.Range(0, blockedPrefabs.Length);
                GameObject gowall = Instantiate(blockedPrefabs[walliIndex], pos, connector.transform.rotation, connector.transform) as GameObject;
                gowall.name = blockedPrefabs[walliIndex].name;
            }
        }

    }

    void CollisionCheck() { 
        BoxCollider box = tileTo.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = tileTo.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
        }
        Vector3 offset = (tileTo.right * box.center.x) + (tileTo.up * box.center.y) + (tileTo.forward * box.center.z);
        Vector3 halfExtents = box.bounds.extents;
        List<Collider> hits = Physics.OverlapBox(tileTo.position + offset, halfExtents, Quaternion.identity, LayerMask.GetMask("Tile")).ToList();
        if (hits.Count > 0)
        {
            if (hits.Exists(x => x.transform != tileFrom && x.transform != tileTo))
            {
                //hit something other than tileTo and tileFrom
                attempts++;
                int toIndex = genneratedTiles.FindIndex(x => x.tile == tileTo);
                if (genneratedTiles[toIndex].connector != null)
                {
                    genneratedTiles[toIndex].connector.isConnected = false;
                }
                genneratedTiles.RemoveAt(toIndex);
                DestroyImmediate(tileTo.gameObject);
                //backtracking
                if (attempts >= maxAttempts)
                {
                    int fromIndex = genneratedTiles.FindIndex(x => x.tile == tileFrom);
                    Tile myTileFrom = genneratedTiles[fromIndex];
                    if (tileFrom != tileRoot)
                    {
                        if(myTileFrom.connector != null)
                        {
                            myTileFrom.connector.isConnected = false;
                        }
                        availableConnectors.RemoveAll(x => x.transform.parent.parent == tileFrom);
                        genneratedTiles.RemoveAt(fromIndex);
                        DestroyImmediate(tileFrom.gameObject);

                        if (myTileFrom.origin != tileRoot)
                        {
                            tileFrom = myTileFrom.origin;
                        }
                        else if (container.name.Contains("Main"))
                        {
                            if(myTileFrom.origin != null)
                            {
                                tileRoot = myTileFrom.origin;
                                tileFrom = tileRoot;
                            }  
                        }
                        else if (availableConnectors.Count > 0)
                        {
                            int availIndex = Random.Range(0, availableConnectors.Count);
                            tileRoot = availableConnectors[availIndex].transform.parent.parent;
                            availableConnectors.RemoveAt(availIndex);
                            tileFrom = tileRoot;
                        }
                        else { return; }
                    }
                    else if (container.name.Contains("Main")){
                        if(myTileFrom.origin != null)
                        {
                            tileRoot = myTileFrom.origin;
                            tileFrom = tileRoot;
                        }
                    }
                    else if(availableConnectors.Count > 0)
                    {
                        int availIndex = Random.Range(0, availableConnectors.Count);
                        tileRoot = availableConnectors[availIndex].transform.parent.parent;
                        availableConnectors.RemoveAt(availIndex);
                        tileFrom = tileRoot;
                    }
                    else { return; }
                }
                //retry
                if (tileFrom != null)
                {
                    if (genneratedTiles.Count == mainLength - 1)
                    {
                        //exit room
                        tileTo = CreateExitTile();
                        DebugRoomLighting(tileTo, Color.magenta);
                    }
                    else
                    {
                        tileTo = CreateTile();
                        Color retryColor = container.name.Contains("Branch") ? Color.green : Color.yellow;
                        DebugRoomLighting(tileTo, retryColor * 2f);

                    }

                    ConnectTiles();
                    CollisionCheck();
                }
            }
            else { attempts = 0; }// nothing other than tileTo and tileFrom was hit(so restore attempts back to zero)   
        }
    }

    void LightRestoration()
    {
        if(useLightsForDebugging && restoreLightsAfterDebugging && Application.isEditor)
        {
            Light[] lights = transform.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
               light.color = startLightColor;
            }
        }
    }

    void CleanupBoxes()
    {
        if (!useBoxColliders)
        {
            foreach(Tile myTile in genneratedTiles)
            {
                BoxCollider box = myTile.tile.GetComponent<BoxCollider>();
                if (box != null)
                {
                    Destroy(box);
                }
            }
        }
    }

    void DebugRoomLighting(Transform tile, Color lightColor)
    {
        if (useLightsForDebugging && Application.isEditor)
        {
            Light[] lights = tile.GetComponentsInChildren<Light>();
            if (lights.Length > 0)
            {
                if (startLightColor == Color.white)
                {
                    startLightColor = lights[0].color;
                }
                foreach (Light light in lights)
                {
                    light.color = lightColor;
                }
            }
        }
    }


    void ConnectTiles()
    {
        Transform connectFrom = GetRandomConnector(tileFrom);
        if (connectFrom == null) { return; }
        Transform connectTo = GetRandomConnector(tileTo);
        if (connectTo == null) { return; }
        connectTo.SetParent(connectFrom);
        tileTo.SetParent(connectTo);
        connectTo.localPosition = Vector3.zero;
        connectTo.localRotation = Quaternion.identity;
        connectTo.Rotate(0, 180f, 0);
        tileTo.SetParent(container);
        connectTo.SetParent(tileTo.Find("Connectors"));
        genneratedTiles.Last().connector = connectFrom.GetComponent<Connector>();

    }

    Transform GetRandomConnector(Transform tile)
    {
        if(tile == null) { return null; }
        List<Connector> connectorList = tile.GetComponentsInChildren<Connector>().ToList().FindAll(x => x.isConnected == false);
        if(connectorList.Count > 0)
        {
            int connectorIndex = Random.Range(0, connectorList.Count);
            connectorList[connectorIndex].isConnected = true;
            if(tile == tileFrom)
            {
                BoxCollider box = tile.GetComponent<BoxCollider>();
                if(box == null)
                {
                    box = tile.gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                }
            }
            return connectorList[connectorIndex].transform;
        }
        return null;
    }

    Transform CreateTile()
    {
        int index = Random.Range(0, tilePrefabs.Length);
        GameObject goTile = Instantiate(tilePrefabs[index], Vector3.zero, Quaternion.identity, container) as GameObject;
        goTile.name = tilePrefabs[index].name;
        Transform origin = genneratedTiles[genneratedTiles.FindIndex(x => x.tile == tileFrom)].tile;
        genneratedTiles.Add(new Tile(goTile.transform, origin));
        return goTile.transform;
    }

    Transform CreateExitTile()
    {
        int index = Random.Range(0, exitPrefabs.Length);
        GameObject goTile = Instantiate(exitPrefabs[index], Vector3.zero, Quaternion.identity, container) as GameObject;
        goTile.name = "Exit Room";
        Transform origin = genneratedTiles[genneratedTiles.FindIndex(x => x.tile == tileFrom)].tile;
        genneratedTiles.Add(new Tile(goTile.transform, origin));
        return goTile.transform;
    }

    Transform CreateStartTile()
    {
        int index = Random.Range(0, startPrefabs.Length);
        GameObject goTile = Instantiate(startPrefabs[index], Vector3.zero, Quaternion.identity, container) as GameObject;
        goTile.name = "Start Room";
        float yRot = Random.Range(0, 4) * 90f;
        goTile.transform.Rotate(0, yRot, 0);
        //add to generated tiles list
        goPlayer.transform.LookAt(goTile.GetComponentInChildren<Connector>().transform);
        genneratedTiles.Add(new Tile(goTile.transform, null));
        return goTile.transform;
    }


    // 퍼즐 룸은 매 판 반드시 생성되어야 한다.
    // 1) 브랜치 끝 → 2) 메인 경로(시작/출구 제외)의 빈 연결부 순서로 겹치지 않는 자리를 찾고,
    // 3) 그래도 실패하면 브랜치 막다른 방 하나를 퍼즐 룸으로 교체한다.
    IEnumerator SpawnPuzzleRoom()
    {
        if (puzzlePrefabs == null || puzzlePrefabs.Length == 0)
        {
            Debug.LogError("PuzzlePrefabs가 비어있습니다. 퍼즐 룸을 생성할 수 없습니다.");
            yield break;
        }

        yield return new WaitForSeconds(constructionDelay);

        GameObject puzzleContainer = new GameObject("PuzzleRoom");
        puzzleContainer.transform.SetParent(transform);
        Transform puzzleParent = puzzleContainer.transform;

        // 1~2단계: 빈 연결부에 배치 시도
        List<Connector> candidates = new List<Connector>();
        List<Connector> mainCandidates = new List<Connector>();
        foreach (Transform child in transform)
        {
            bool isBranch = child.name.Contains("Branch");
            bool isMain = child.name.Contains("Main");
            if (!isBranch && !isMain) continue;
            foreach (Connector c in child.GetComponentsInChildren<Connector>())
            {
                if (c.isConnected) continue;
                Transform owner = c.transform.parent != null ? c.transform.parent.parent : null;
                if (owner == null) continue;
                if (owner.name == "Start Room" || owner.name == "Exit Room") continue;
                if (isBranch) candidates.Add(c); else mainCandidates.Add(c);
            }
        }
        Shuffle(candidates);
        Shuffle(mainCandidates);
        candidates.AddRange(mainCandidates);

        foreach (Connector c in candidates)
        {
            if (TryPlacePuzzle(c, puzzleParent)) yield break;
        }

        // 3단계: 브랜치 막다른 방을 제거하고 그 자리에 퍼즐 룸 배치
        List<Tile> leafTiles = genneratedTiles.FindAll(t =>
            t.tile != null && t.connector != null &&
            t.tile.parent != null && t.tile.parent.name.Contains("Branch") &&
            t.tile.GetComponentsInChildren<Connector>().Count(x => x.isConnected) <= 1);
        Shuffle(leafTiles);

        foreach (Tile leaf in leafTiles)
        {
            Connector originConnector = leaf.connector;
            Transform leafTransform = leaf.tile;
            genneratedTiles.Remove(leaf);
            DestroyImmediate(leafTransform.gameObject);
            originConnector.isConnected = false;
            Physics.SyncTransforms();

            if (TryPlacePuzzle(originConnector, puzzleParent)) yield break;
        }

        Debug.LogError("퍼즐 룸을 배치할 자리를 찾지 못했습니다.");
        Destroy(puzzleContainer);
    }

    bool TryPlacePuzzle(Connector fromConnector, Transform puzzleParent)
    {
        Transform from = fromConnector.transform.parent.parent;
        int fromIndex = genneratedTiles.FindIndex(x => x.tile == from);
        if (fromIndex < 0) return false;

        // 프리팹 순서와 프리팹의 연결부를 모두 시도
        List<int> prefabOrder = Enumerable.Range(0, puzzlePrefabs.Length).ToList();
        Shuffle(prefabOrder);

        foreach (int prefabIndex in prefabOrder)
        {
            int connectorCount = puzzlePrefabs[prefabIndex].GetComponentsInChildren<Connector>(true).Length;
            for (int ci = 0; ci < connectorCount; ci++)
            {
                GameObject goPuzzle = Instantiate(puzzlePrefabs[prefabIndex], Vector3.zero, Quaternion.identity, puzzleParent);
                goPuzzle.name = "Puzzle Room";
                Transform puzzle = goPuzzle.transform;
                Connector[] puzzleConnectors = puzzle.GetComponentsInChildren<Connector>(true);
                Transform connectTo = puzzleConnectors[ci].transform;
                Transform connectorsRoot = connectTo.parent;

                connectTo.SetParent(fromConnector.transform);
                puzzle.SetParent(connectTo);
                connectTo.localPosition = Vector3.zero;
                connectTo.localRotation = Quaternion.identity;
                connectTo.Rotate(0, 180f, 0);
                puzzle.SetParent(puzzleParent);
                connectTo.SetParent(connectorsRoot);
                Physics.SyncTransforms();

                if (!OverlapsOtherTiles(puzzle, from))
                {
                    fromConnector.isConnected = true;
                    puzzleConnectors[ci].isConnected = true;

                    BoxCollider fromBox = from.GetComponent<BoxCollider>();
                    if (fromBox == null)
                    {
                        fromBox = from.gameObject.AddComponent<BoxCollider>();
                        fromBox.isTrigger = true;
                    }

                    Tile puzzleTile = new Tile(puzzle, from);
                    puzzleTile.connector = fromConnector;
                    genneratedTiles.Add(puzzleTile);

                    DebugRoomLighting(puzzle, Color.cyan);
                    Debug.Log($"퍼즐 룸이 [{from.name}]에 연결되어 생성되었습니다.");
                    return true;
                }

                DestroyImmediate(goPuzzle);
            }
        }
        return false;
    }

    bool OverlapsOtherTiles(Transform tile, Transform ignore)
    {
        BoxCollider box = tile.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = tile.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Physics.SyncTransforms();
        }
        Vector3 offset = (tile.right * box.center.x) + (tile.up * box.center.y) + (tile.forward * box.center.z);
        Vector3 halfExtents = box.bounds.extents;
        Collider[] hits = Physics.OverlapBox(tile.position + offset, halfExtents, Quaternion.identity, LayerMask.GetMask("Tile"));
        foreach (Collider hit in hits)
        {
            if (hit.transform == tile || hit.transform == ignore) continue;
            if (hit.transform.IsChildOf(tile)) continue;
            return true;
        }
        return false;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

}
