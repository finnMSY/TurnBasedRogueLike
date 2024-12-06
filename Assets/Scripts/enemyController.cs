using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class enemyController : MonoBehaviour {
    public tilemapManager tileManager;
    public int numActions;
    List<Tile> totalTiles = new List<Tile>();
    GameObject playerObject;
    public turnController turnController;

    public void startTurn() {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        List<Tile> quickestPath = findQuickestPath(tileManager.FindTile(new Vector3Int(3, 2, 0)), playerObject.GetComponent<characterController>().currentTile);
        foreach(Tile t in quickestPath) {
            Debug.Log(t.position);
        }
        turnController.endOfTurn();
    }

    List<Tile> findQuickestPath(Tile startingTile, Tile destinationTile) {
        Queue<Tile> nodeQueue = new Queue<Tile>();
        HashSet<Tile> visitedTiles = new HashSet<Tile>();
        Dictionary<Tile, Tile> tileParents = new Dictionary<Tile, Tile>();

        nodeQueue.Enqueue(startingTile);
        visitedTiles.Add(startingTile);

        while(nodeQueue.Count > 0) {
            Tile tile = nodeQueue.Dequeue();
            if (tile == destinationTile) {
                List<Tile> path = new List<Tile>();
                do {
                    path.Add(tile);
                    if (tileParents.ContainsKey(tile)) {
                        tile = tileParents[tile];
                    }
                    else {
                        tile = null;
                    }

                }
                while(tile != null);

                path.Reverse();     
                return(path);
                
            }
            else {
                foreach(Tile neighbour in tile.neighbours) {
                    if (!visitedTiles.Contains(neighbour) && !neighbour.occupied) {
                        tileParents[neighbour] = tile;
                        nodeQueue.Enqueue(neighbour);
                        visitedTiles.Add(neighbour);
                    }
                }
            }
        }
        return (null);
    }

    void Update() {
    
    }
}
