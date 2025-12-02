using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonGenerator : MonoBehaviour
{
    public GameObject[] tilePrefabs;
    public GameObject[] startPrefabs;
    public GameObject[] exitPrefabs;
    public GameObject[] blockedPrefabs;
    public GameObject[] doorPrefabs;

    [Header("Debugging Options")]
    public bool useBoxColliders;
    public bool useLightsForDebugging;
    public bool restoreLightsAfterDebugging;


    [Header("Key Bindings")]
    public KeyCode reloadKey = KeyCode.Backspace;
    public KeyCode toggleMapKey = KeyCode.M;


    [Header("Generation Limits")]
    [UnityEngine.Range(2,100)]public int mainLength = 10;
    [UnityEngine.Range(0, 50)] public int branchLength = 5;
    [UnityEngine.Range(0, 25)] public int numBranches = 10;
    [UnityEngine.Range(0, 100)] public int doorPercent = 25;
    [UnityEngine.Range(0, 1f)] public float constructionDelay;

    [Header("Availabe at Runtime")]
    public List<Tile> genneratedTiles = new List<Tile>();

    GameObject goCamera, goPlayer;
    List<Connector> availableConnectors = new List<Connector>();
    Color startLightColor = Color.white;
    Transform tileFrom, tileTo, tileRoot;
    Transform container;
    int attempts;
    int maxAttempts = 50;

    void Start()
    {
        goCamera = GameObject.Find("OverheadCamera");       
        goPlayer = GameObject.FindWithTag("Player");       
        StartCoroutine(DungeonBuild());

    }
    private void Update()
    {
        if (Input.GetKeyDown(reloadKey))
        {
            SceneManager.LoadScene("Game");
        }
        if(Input.GetKeyDown(toggleMapKey))
        {
            goCamera.SetActive(!goCamera.activeInHierarchy);
            goPlayer.SetActive(!goCamera.activeInHierarchy);
        }
    }

    IEnumerator DungeonBuild()
    {
        goCamera.SetActive(true);
        goPlayer.SetActive(false);
        GameObject goContainer = new GameObject("Main Path");
        container = goContainer.transform;
        container.SetParent(transform);
        tileRoot = CreateStartTile();
        DebugRoomLighting(tileRoot, Color.blue);
        tileTo = tileRoot;
        for (int i = 0; i < mainLength - 1; i++)
        {
            yield return new WaitForSeconds(constructionDelay);
            tileFrom = tileTo;
            tileTo = CreateTile();
            DebugRoomLighting(tileTo, Color.yellow);
            ConnectTiles();
            CollisionCheck();
            if (attempts >= maxAttempts) { break; }
        }
        //get all connectors within container that not already connected
        foreach(Connector connector in container.GetComponentsInChildren<Connector>())
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
        LightRestoration();
        CleanupBoxes();
        goCamera.SetActive(false);
        goPlayer.SetActive(true);
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
                    tileTo = CreateTile();
                    Color retryColor = container.name.Contains("Branch") ? Color.green : Color.yellow;
                    DebugRoomLighting(tileTo, retryColor * 2f);
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

   
}
