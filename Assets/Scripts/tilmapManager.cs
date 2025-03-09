using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class tilemapManager : MonoBehaviour
{
    public List<tilemapGenerator> tilemapGenerators;

    public List<Tile> totalTiles = new List<Tile>();
    public GameObject enemies;
    public GameObject players;

    void Start() {
        foreach (tilemapGenerator gen in tilemapGenerators) {
            totalTiles.AddRange(gen.getTiles());
        }   
        AddNeighbours(totalTiles);
        players.GetComponent<characterController>().enabled = true;

        foreach(Transform child in enemies.transform) {
            child.GetComponent<enemyController>().enabled = true;
        }
    }

    void AddNeighbours(List<Tile> tiles) {
        foreach (Tile tile in tiles) {
            List<Tile> neighbours = new List<Tile>();
            Vector3Int position = tile.position;
            AddNeighbouringTile(new Vector3Int(position.x, position.y+1, position.z), neighbours);
            AddNeighbouringTile(new Vector3Int(position.x, position.y-1, position.z), neighbours);
            AddNeighbouringTile(new Vector3Int(position.x+1, position.y, position.z), neighbours);
            AddNeighbouringTile(new Vector3Int(position.x-1, position.y, position.z), neighbours);
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
        return(totalTiles);
    }

    public Tile FindTile(Vector3Int position) {
        foreach (Tile tile in totalTiles) {
            if (tile.position == position) {
                return (tile);
            }
        }
        return(null);
    }
}
