using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class tilemapManager : MonoBehaviour
{
    public List<tilemapGenerator> tilemapGenerators;

    public List<Tile> totalTiles = new List<Tile>();
    GameObject enemies;
    GameObject players;

    void Awake() {
        InitialiseTiles();
        enemies = GameObject.Find("Enemies");
        players = GameObject.FindWithTag("Player");
    }

    public void InitialiseTiles() {
        if (totalTiles.Count > 0) return;

        foreach (tilemapGenerator gen in tilemapGenerators) {
            gen.GenerateTiles();
            totalTiles.AddRange(gen.getTiles());
        }

        AddNeighbours(totalTiles);
    }

    void Start() {
        players.GetComponent<characterController>().enabled = true;

        foreach(Transform child in enemies.transform) {
            child.GetComponent<enemyController>().enabled = true;
        }

        roomController roomController = gameObject.GetComponent<roomController>();
        foreach(Transform door in roomController.doors.transform) {
            door.GetComponent<door>().enabled = true;
        }
    }

    public void SwtichTileObstacleStatus(Vector3Int tileCoords, bool setToOccupied)
    {
        Tile tile = FindTile(tileCoords);

        if (tile == null)
        {
            Debug.LogWarning($"SwtichTileObstacleStatus: tile {tileCoords} not found.");
            return;
        }
        tile.occupied = setToOccupied;
    }

    void AddNeighbours(List<Tile> tiles) {
        foreach (Tile tile in tiles)
        {
            List<Tile> neighbours = new List<Tile>();
            Vector3Int position = tile.position;
            AddNeighbouringTile(new Vector3Int(position.x, position.y + 1, position.z), neighbours);
            AddNeighbouringTile(new Vector3Int(position.x, position.y - 1, position.z), neighbours);
            AddNeighbouringTile(new Vector3Int(position.x + 1, position.y, position.z), neighbours);
            AddNeighbouringTile(new Vector3Int(position.x - 1, position.y, position.z), neighbours);
            tile.setNeighbours(neighbours);
        }
    }

    void AddNeighbouringTile(Vector3Int position, List<Tile> neighbours) { 
        Tile tile = FindTile(position);
        if (tile != null) {
            neighbours.Add(tile);
        }
    }

    public List<Tile> getTilesList() {
        return totalTiles;
    }

    public Tile FindTile(Vector3Int position) {
        foreach (Tile tile in totalTiles)
        {
            if (tile.position == position)
            {
                return tile;
            }
        }
        return null;
    }
}