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
        // Debug.Log(pos);
        tilemap.SetTile(pos, null);
    }

    // Start is called before the first frame update
    void Start() {
        bounds = tilemap.cellBounds;

        TileBase[] tiles = tilemap.GetTilesBlock(bounds);

        // removeTile(new Vector3Int(2, -1, 0));
        for (int x = bounds.xMin; x < bounds.xMax; x++) {
            for (int y = bounds.yMin; y < bounds.yMax; y++) {
                TileBase selectedTile = getTile(new Vector3Int(x, y, 0));
                if (selectedTile != null) {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    List<Tile> neighbours = new List<Tile>();
                    Tile tileObject = new Tile(position, selectedTile, tilesOccupied, neighbours);
                    // Debug.Log(transform.name + tileObject.occupied);
                    allTiles.Add(tileObject);
                }
            } 
        }
        tilemapManager tileManager = gameObject.transform.parent.GetComponent<tilemapManager>();
        if (tileManager.enabled == false) {
            tileManager.enabled = true;
        }
    }

    public TileBase getTile(Vector3Int pos) {
        return(tilemap.GetTile(pos));
    }

    public List<Tile> getTiles() {   
        foreach(Tile t in allTiles) {
            //Debug.Log(t.occupied);
        }
        return (allTiles);
    }
}
