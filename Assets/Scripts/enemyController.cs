using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class enemyController : MonoBehaviour {
    public tilemapManager tileManager;
    public int numActions;
    List<Tile> totalTiles = new List<Tile>();
    GameObject playerObject;
    public turnController turnController;
    Dictionary<Tile, int> tileActionPoints;

    Tile currentTile;
    [SerializeField]
    private Transform movePoint;
    [SerializeField]
    private float speed = 5;

    public void Start() {
        this.currentTile = tileManager.FindTile(new Vector3Int(3, 2, 0));
        movePoint.parent = null;
    }

    public void startTurn() {
        tileActionPoints = new Dictionary<Tile, int>(); 
        playerObject = GameObject.FindGameObjectWithTag("Player");
        List<Tile> quickestPath = findQuickestPath(currentTile, playerObject.GetComponent<characterController>().currentTile);
        List<Tile> totalMoveableTiles = findTotalMoveableTiles(currentTile, numActions);
        for(int i = 0; i < quickestPath.Count; i++) {
            tileActionPoints.Add(quickestPath[i], i);
        }
        foreach(Tile t in totalMoveableTiles) {
            if (!tileActionPoints.ContainsKey(t)) {
                tileActionPoints.Add(t, 0);
            }
        }
        
        for (int i = 0; i < numActions; i++) {
            moveEnemy(currentTile);
        }
        turnController.endOfTurn();
    }

    List<Tile> findTotalMoveableTiles(Tile startingTile, int maxLevels) {
        List<Tile> totalMoveableTiles = new List<Tile>();

        Queue<(Tile, int)> bfsQueue = new Queue<(Tile, int)>();
        bfsQueue.Enqueue((startingTile, 0));

        while (bfsQueue.Count > 0) {
            var currentTuple = bfsQueue.Dequeue();
            Tile currentTile = currentTuple.Item1;
            int currentDepth = currentTuple.Item2;

            if (currentDepth <= maxLevels && !totalMoveableTiles.Contains(currentTile) && !currentTile.occupied) {
                totalMoveableTiles.Add(currentTile);
                int count = 1;
                foreach (Tile neighbouringTile in currentTile.neighbours) {
                    count++;
                    bfsQueue.Enqueue((neighbouringTile, currentDepth + 1));
                }
            }
        }
        return totalMoveableTiles;    
    }

    void moveEnemy(Tile currentTile) {
        var highestPointTile = (currentTile, tileActionPoints[currentTile]);
        foreach (Tile neighbouringTile in currentTile.neighbours) {
            if (!neighbouringTile.occupied) {
                int tilePoints = tileActionPoints[neighbouringTile];
                if (tilePoints > highestPointTile.Item2) {
                    highestPointTile = (neighbouringTile, tilePoints);
                }
            }
        }
        Debug.Log(highestPointTile);
        if (moveGameObject(this.currentTile, highestPointTile.Item1)) {
            this.currentTile = highestPointTile.Item1;
            Debug.Log(this.currentTile.position);
        }
    }

    bool moveGameObject(Tile currentTile, Tile newTile) {
        float moveDistance = playerObject.GetComponent<characterController>().moveDistance;
        if (currentTile != null && newTile != null) {
            Vector3 direction = Vector3.zero;

            if (newTile.position.x > currentTile.position.x) {
                movePoint.position += new Vector3(moveDistance, 0, 0);
            }
            else if (newTile.position.x < currentTile.position.x) {
                movePoint.position += new Vector3(-moveDistance, 0, 0); 
            }
            else if (newTile.position.y > currentTile.position.y) {
                movePoint.position += new Vector3(0, moveDistance, 0); 
            }
            else if (newTile.position.y < currentTile.position.y) {
                movePoint.position += new Vector3(0, -moveDistance, 0); 
            }

            // if (Vector3.Distance(transform.position, movePoint.position) <= 0.05f) {
            return true;
            // }
        } else {
            Debug.LogError("CurrentTile or NewTile is null");
        } 
        return false;
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
        float movementAmount = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, movePoint.position, movementAmount);
    }
}
