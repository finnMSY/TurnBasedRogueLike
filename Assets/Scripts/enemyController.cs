using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class enemyController : MonoBehaviour {

    private tilemapManager tileManager;
    public int numActions;
    private List<Tile> totalTiles = new List<Tile>();
    private GameObject playerObject;
    private gameController turnController;
    private Dictionary<Tile, int> tileActionPoints;
    
    [HideInInspector]
    public EnemyType enemyType;
    public float speed = 5;
    public double damageMultiplyer = 1;
    public int health = 100;

    [HideInInspector]
    public List<string> moveSet = new List<string>();

    private Tile currentTile;
    private Transform movePoint;

    public void Start() {
        movePoint = transform.Find("movePoint").transform;
        tileManager = FindObjectOfType<tilemapManager>();
        turnController = FindObjectOfType<gameController>();
        this.currentTile = tileManager.FindTile(new Vector3Int(3, 2, 0));
        movePoint.parent = null;
    }

    public List<string> GetMovesForEnemyType(EnemyType type) {
        turnController = FindObjectOfType<gameController>();
        List<string> moves = new List<string>();
        
        foreach (MoveSet set in turnController.moveSets) {
            if (set.enemyType == type) {
                foreach (Ability ab in set.moves) {
                    moves.Add(ab.name);
                }
                break; 
            }
        }

        return moves; // Return the list of moves
    }

    public void takeDamage(int damage)
    {
        health -= damage;
    }

    public void startTurn()
    {
        tileActionPoints = new Dictionary<Tile, int>();
        playerObject = GameObject.FindGameObjectWithTag("Player");

        List<Tile> quickestPath = findQuickestPath(currentTile, playerObject.GetComponent<characterController>().currentTile);
        List<Tile> totalMoveableTiles = findTotalMoveableTiles(currentTile, numActions);

        assignPoints(tileActionPoints, quickestPath, totalMoveableTiles);
        StartCoroutine(moveEnemyRoutine());
    }

    void assignPoints(Dictionary<Tile, int> tileActionPoints, List<Tile> quickestPath, List<Tile >totalMoveableTiles) {
        for(int i = 0; i < quickestPath.Count; i++) {
            tileActionPoints.Add(quickestPath[i], i);
        }

        // In range to cause damage: get a point per potenial damage dealt

        foreach(Tile t in totalMoveableTiles) {
            if (!tileActionPoints.ContainsKey(t)) {
                tileActionPoints.Add(t, 0);
            }
        }
    }

    IEnumerator moveEnemyRoutine()
    {
        for (int i = 0; i < numActions; i++)
        {
            moveEnemy();
            yield return new WaitForSeconds(.4f);   
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

   void moveEnemy()
    {
        var bestTile = (currentTile, tileActionPoints[currentTile]);

        foreach (Tile neighbour in currentTile.neighbours)
        {
            if (!neighbour.occupied && tileActionPoints.TryGetValue(neighbour, out int points) && points > bestTile.Item2)
            {
                bestTile = (neighbour, points);
            }
        }

        if (moveGameObject(currentTile, bestTile.Item1))
        {
            currentTile = bestTile.Item1;
            // Debug.Log($"Enemy moved to: {currentTile.position}");
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
