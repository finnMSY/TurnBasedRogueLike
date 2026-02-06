using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json; 
using System.Linq;
using Newtonsoft.Json.Linq;


public class enemyController : MonoBehaviour {

    private tilemapManager tileManager;
    public int numActions;
    private List<Tile> totalTiles = new List<Tile>();
    private GameObject playerObject;
    private gameController turnController;
    private Dictionary<Tile, int> tileActionPoints;
    private Dictionary<Ability, int> abilityActionPoints;
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

    public (KeyValuePair<Tile, int>? dict, Tile key) GetHighestTileDictionary()
    {
        if (tileActionPoints.Count == 0) 
            return (null, null);
        
        var maxPair = tileActionPoints.OrderByDescending(x => x.Value).First();
        return (maxPair, maxPair.Key);
    }

    public (KeyValuePair<Ability, int>? dict, Ability key) GetHighestAbilityDictionary()
    {
        if (abilityActionPoints.Count == 0) 
            return (null, null);
        
        var maxPair = abilityActionPoints.OrderByDescending(x => x.Value).First();
        return (maxPair, maxPair.Key);
    }

    public List<Ability> GetMovesForEnemyType(EnemyType type) {
        turnController = FindObjectOfType<gameController>();
        List<Ability> moves = new List<Ability>();
        
        foreach (MoveSet set in turnController.moveSets) {
            if (set.enemyType == type) {
                foreach (Ability ab in set.moves) {
                    moves.Add(ab);
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

    private void attack_or_move()
    {
        // Get both results
        var (tileDict, tileKey) = GetHighestTileDictionary();
        var (abilityDict, abilityKey) = GetHighestAbilityDictionary();
        
        // Get the values for comparison
        int tileValue = tileKey != null ? tileActionPoints[tileKey] : 0;
        int abilityValue = abilityKey != null ? abilityActionPoints[abilityKey] : 0;
        

        if (tileValue > abilityValue)
        {
            StartCoroutine(moveEnemyRoutine());
        }
        else
        {
            // Use ability logic here
            Debug.Log($"Use {abilityKey.name} for {abilityValue} damage.");
        }
    }

    public void startTurn()
    {
        tileActionPoints = new Dictionary<Tile, int>();
        abilityActionPoints = new Dictionary<Ability, int>();
        playerObject = GameObject.FindGameObjectWithTag("Player");

        List<Tile> quickestPath = findQuickestPath(currentTile, playerObject.GetComponent<characterController>().currentTile);
        List<Tile> totalMoveableTiles = findTotalMoveableTiles(currentTile, numActions);

        assignPoints(tileActionPoints, quickestPath, totalMoveableTiles);
        attack_or_move();

        turnController.endOfTurn();
    }

    void assignPoints(Dictionary<Tile, int> tileActionPoints, List<Tile> quickestPath, List<Tile >totalMoveableTiles) {
        int startIndex = Mathf.Max(0, quickestPath.Count - numActions);
        for(int i = startIndex; i < quickestPath.Count; i++) {
            tileActionPoints.Add(quickestPath[i], i);
        }

        // In range to cause damage: get a point per potenial damage dealt
        var moveSet = GetMovesForEnemyType(enemyType);

        foreach (Ability a in moveSet)
        {
            int damage = a.damage;
            Dictionary<string, object> range = JsonConvert.DeserializeObject<Dictionary<string, object>>(a.range.text);
            JArray tilesArray = JArray.Parse(range["tiles"].ToString());
            
            foreach (JObject tile in tilesArray)
            {
                int x = (int)tile["x"];
                int y = (int)tile["y"];
                
                // Convert relative coordinates to world position
                Vector3Int targetPos = new Vector3Int(
                    currentTile.position.x + x,
                    currentTile.position.y + y,
                    0
                );
                
                Tile targetTile = tileManager.FindTile(targetPos);
                if (targetTile != null)
                {
                    // Add points based on damage
                    if (tileActionPoints.ContainsKey(targetTile))
                    {
                        abilityActionPoints[a] = damage;
                    }
                }
            }
        }

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
        // Find the tile with highest score anywhere
        Tile targetTile = tileActionPoints.OrderByDescending(kv => kv.Value).First().Key;

        if (targetTile == currentTile) return; // Already on the best tile

        // Find quickest path to that tile
        List<Tile> path = findQuickestPath(currentTile, targetTile);

        if (path == null || path.Count < 2) return; // No movement possible

        Tile nextTile = path[1]; // The next step along the path

        if (moveGameObject(currentTile, nextTile))
        {
            currentTile = nextTile;
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
