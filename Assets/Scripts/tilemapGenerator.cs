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

    // Start is called before the first frame update
    void Start() {

        bounds = tilemap.cellBounds;

        TileBase[] tiles = tilemap.GetTilesBlock(bounds);

        for (int x = bounds.xMin; x < bounds.xMax; x++) {
            for (int y = bounds.yMin; y < bounds.yMax; y++) {
                TileBase selectedTile = getTile(new Vector3Int(x, y, 0));

                if (selectedTile != null) {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    List<Tile> neighbours = new List<Tile>();
                    Tile tileObject = new Tile(position, selectedTile, tilesOccupied, neighbours);
                    allTiles.Add(tileObject);
                }
            } 
        }
    }

    public TileBase getTile(Vector3Int pos) {
        return(tilemap.GetTile(pos));
    }

    // Update is called once per frame
    public List<Tile> getTiles() {   
        Debug.Log(allTiles);
        return (allTiles);
    }
}
