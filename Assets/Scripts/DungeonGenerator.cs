using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonGenerator : MonoBehaviour
{
    public GameObject[] tilePrefabs;
    public GameObject[] startPrefabs;
    public KeyCode reloadKey = KeyCode.Backspace;
    public List<Tile> genneratedTiles = new List<Tile>();


    Transform tileFrom, tileTo;


    void Start()
    {
        tileFrom = CreateStartTile();
        tileTo = CreateTile();
        ConnectTiles();
        for(int i = 0; i < 10; i++)
        {
            tileFrom = tileTo;
            tileTo = CreateTile();
            ConnectTiles();
        }

    }

    private void Update()
    {
        if (Input.GetKeyDown(reloadKey))
        {
            SceneManager.LoadScene("Game");
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
        tileTo.SetParent(transform);
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
            return connectorList[connectorIndex].transform;
        }
        return null;
    }

    Transform CreateTile()
    {
        int index = Random.Range(0, tilePrefabs.Length);
        GameObject goTile = Instantiate(tilePrefabs[index], Vector3.zero, Quaternion.identity, transform) as GameObject;
        goTile.name = tilePrefabs[index].name;
        Transform origin = genneratedTiles[genneratedTiles.FindIndex(index => index.tile == tileFrom)].tile;
        genneratedTiles.Add(new Tile(goTile.transform, origin));
        return goTile.transform;
    }

    Transform CreateStartTile()
    {
        int index = Random.Range(0, startPrefabs.Length);
        GameObject goTile = Instantiate(startPrefabs[index], Vector3.zero, Quaternion.identity, transform) as GameObject;
        goTile.name = "Start Room";
        float yRot = Random.Range(0, 4) * 90f;
        goTile.transform.Rotate(0, yRot, 0);
        //add to generated tiles list
        genneratedTiles.Add(new Tile(goTile.transform, null));
        return goTile.transform;
    }

   



}
