using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Tile
{
    public Vector3Int position;
    public TileBase tileObject;
    public bool occupied;
    public List<Tile> neighbours;

    public Tile(Vector3Int position, TileBase tileObject, bool occupied, List<Tile> neighbours = null) {
        this.tileObject = tileObject;
        this.position = position;
        this.occupied = occupied;
        this.neighbours = neighbours != null ? neighbours : new List<Tile>();
    }

    public void setNeighbours(List<Tile> neighbours) {
        this.neighbours = neighbours;
    }
    
}
