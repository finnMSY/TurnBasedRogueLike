using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class tilemapGenerator : MonoBehaviour
{
    public Tilemap tilemap; 
    BoundsInt bounds;
    public List<Tile> allTiles = new List<Tile>();
    public bool tilesOccupied;

    public void removeTile(Vector3Int pos) {
        tilemap.SetTile(pos, null);
    }

    void Start() {
        tilemapManager tileManager = gameObject.transform.parent.GetComponent<tilemapManager>();
        if (tileManager.enabled == false) {
            tileManager.enabled = true;
        }
    }

    public void GenerateTiles() {
        if (allTiles.Count > 0) return;

        bounds = tilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++) {
            for (int y = bounds.yMin; y < bounds.yMax; y++) {
                TileBase selectedTile = getTile(new Vector3Int(x, y, 0));
                if (selectedTile != null) {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    Tile tileObject = new Tile(position, selectedTile, tilesOccupied, new List<Tile>());
                    allTiles.Add(tileObject);
                }
            } 
        }
    }

    public TileBase getTile(Vector3Int pos) {
        return tilemap.GetTile(pos);
    }

    public List<Tile> getTiles() {   
        return allTiles;
    }
}