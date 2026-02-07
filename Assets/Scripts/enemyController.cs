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
    private SpriteRenderer spriteRenderer;
    public float flashDuration = 0.1f; 
    private Color originalColor;
    private List<Tile> totalTiles = new List<Tile>();
    private GameObject playerObject;
    private GameObject attackObject;
    private gameController turnController;
    private Dictionary<Tile, int> tileActionPoints;
    private Dictionary<Ability, int> abilityActionPoints;
    public EnemyType enemyType;
    public float speed = 5;
    public double damageMultiplyer = 1;
    public int health = 100;
    public Vector3Int startingTile = new Vector3Int(3, 2, 0);

    [HideInInspector]
    public List<string> moveSet = new List<string>();
 
    private Tile currentTile;
    private Transform movePoint;

    private void Start() 
    {
        movePoint = transform.Find("movePoint").transform;
        tileManager = FindObjectOfType<tilemapManager>();
        turnController = FindObjectOfType<gameController>();
        playerObject = GameObject.FindGameObjectWithTag("Player");
        this.currentTile = tileManager.FindTile(startingTile);
        movePoint.parent = null;

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        
        // Mark starting tile as occupied
        if (currentTile != null)
        {
            currentTile.occupied = true;
        }
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
        List<Ability> moves = new List<Ability>();
        
        foreach (MoveSet set in turnController.moveSets) {
            if (set.enemyType == type) {
                foreach (Ability ab in set.moves) {
                    moves.Add(ab);
                }
                break; 
            }
        }

        return moves;
    }

    public void takeDamage(int damage)
    {
        Debug.Log($"Dealt {damage} damage!");
        health -= damage;

        if (health <= 0)
        {
            die();
        }
        else
        {
            StartCoroutine(FlashRed());
        }
    }

    IEnumerator FlashRed() {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    void die()
    {
        // Free up the tile when enemy dies
        if (currentTile != null)
        {
            currentTile.occupied = false;
        }
        Destroy(this.gameObject);
    }

    public void startTurn()
    {
        StartCoroutine(ExecuteTurn());
    }

    private IEnumerator ExecuteTurn()
    {        
        tileActionPoints = new Dictionary<Tile, int>();
        abilityActionPoints = new Dictionary<Ability, int>();
        
        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogError("Player object not found!");
            turnController.endOfTurn();
            yield break;
        }

        characterController playerController = playerObject.GetComponent<characterController>();
        if (playerController == null || playerController.currentTile == null)
        {
            Debug.LogError("Player controller or current tile not found!");
            turnController.endOfTurn();
            yield break;
        }

        List<Tile> quickestPath = findQuickestPath(currentTile, playerController.currentTile);
        List<Tile> totalMoveableTiles = findTotalMoveableTiles(currentTile, numActions);

        assignPoints(quickestPath, totalMoveableTiles);
        
        // Wait for movement/attack to complete
        yield return StartCoroutine(attack_or_move_coroutine());

        // Cleanup
        if (tileActionPoints != null)
            tileActionPoints.Clear();
        if (abilityActionPoints != null)
            abilityActionPoints.Clear();
        
        // Now end turn
        turnController.endOfTurn();
    }

    private void Attack(Ability ability, int damageAmount)
    {
        Debug.Log($"Use {ability.name} for {damageAmount} damage.");

        characterController player = playerObject.GetComponent<characterController>();
        
        Vector3 direction = (player.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        ability.animation.GetComponent<enemyAnimationController>().damage = damageAmount;
        attackObject = Instantiate(ability.animation, transform.position, Quaternion.identity * rotation, transform);
    }

    private IEnumerator attack_or_move_coroutine()
    {
        var (tileDict, tileKey) = GetHighestTileDictionary();
        var (abilityDict, abilityKey) = GetHighestAbilityDictionary();
        
        int tileValue = tileKey != null ? tileActionPoints[tileKey] : 0;
        int abilityValue = abilityKey != null ? abilityActionPoints[abilityKey] : 0;
        
        if (tileValue > abilityValue)
        {
            yield return StartCoroutine(moveEnemyRoutine());
        }
        else if (abilityKey != null)
        {
            Attack(abilityKey, abilityValue);
        }
    }

    void assignPoints(List<Tile> quickestPath, List<Tile> totalMoveableTiles) 
    {
        if (quickestPath == null || playerObject == null) return;
        
        int startIndex = Mathf.Max(0, quickestPath.Count - numActions);
        for(int i = startIndex; i < quickestPath.Count; i++) {
            if (!tileActionPoints.ContainsKey(quickestPath[i]))
            {
                tileActionPoints.Add(quickestPath[i], i);
            }
        }

        // In range to cause damage: get a point per potential damage dealt
        var moveSet = GetMovesForEnemyType(enemyType);
        characterController playerController = playerObject.GetComponent<characterController>();
        if (playerController == null) return;
        
        Tile playerTile = playerController.currentTile;
        if (playerTile == null) return;

        foreach (Ability a in moveSet)
        {
            int damage = a.damage;
            Dictionary<string, object> range = JsonConvert.DeserializeObject<Dictionary<string, object>>(a.range.text);
            JArray tilesArray = JArray.Parse(range["tiles"].ToString());
            
            // Check if player is within ability range from current position
            bool playerInRange = false;
            
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
                if (targetTile != null && targetTile == playerTile)
                {
                    playerInRange = true;
                    break;
                }
            }
            
            // Assign points if player is in range
            if (playerInRange)
            {
                if (abilityActionPoints.ContainsKey(a))
                {
                    abilityActionPoints[a] = Mathf.Max(abilityActionPoints[a], damage);
                }
                else
                {
                    abilityActionPoints.Add(a, damage);
                }
            }
        }

        if (totalMoveableTiles != null)
        {
            foreach(Tile t in totalMoveableTiles) {
                if (!tileActionPoints.ContainsKey(t)) {
                    tileActionPoints.Add(t, 0);
                }
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
        HashSet<Tile> visited = new HashSet<Tile>();

        while (bfsQueue.Count > 0) {
            var currentTuple = bfsQueue.Dequeue();
            Tile tile = currentTuple.Item1;
            int currentDepth = currentTuple.Item2;

            if (visited.Contains(tile))
                continue;
                
            visited.Add(tile);

            // Allow current tile or unoccupied tiles
            if (currentDepth <= maxLevels && (tile == startingTile || !tile.occupied)) {
                totalMoveableTiles.Add(tile);
                
                foreach (Tile neighbouringTile in tile.neighbours) {
                    if (!visited.Contains(neighbouringTile))
                    {
                        bfsQueue.Enqueue((neighbouringTile, currentDepth + 1));
                    }
                }
            }
        }
        return totalMoveableTiles;    
    }

    void moveEnemy()
    {
        if (tileActionPoints == null || tileActionPoints.Count == 0) return;

        // Find the tile with highest score anywhere
        Tile targetTile = tileActionPoints.OrderByDescending(kv => kv.Value).First().Key;

        if (targetTile == currentTile) return; // Already on the best tile

        // Find quickest path to that tile
        List<Tile> path = findQuickestPath(currentTile, targetTile);

        if (path == null || path.Count < 2) return; // No movement possible

        Tile nextTile = path[1]; // The next step along the path

        // Check if next tile is occupied
        if (nextTile.occupied && nextTile != currentTile)
        {
            Debug.Log($"Enemy {gameObject.name} can't move - tile occupied");
            return;
        }

        if (moveGameObject(currentTile, nextTile))
        {
            // Free previous tile and occupy new tile
            if (currentTile != null)
            {
                currentTile.occupied = false;
            }
            
            currentTile = nextTile;
            
            if (currentTile != null)
            {
                currentTile.occupied = true;
            }
        }
    }

    bool moveGameObject(Tile currentTile, Tile newTile) {
        if (playerObject == null) return false;
        
        characterController playerController = playerObject.GetComponent<characterController>();
        if (playerController == null) return false;
        
        float moveDistance = playerController.moveDistance;
        
        if (currentTile != null && newTile != null) {
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

            return true;
        } else {
            Debug.LogError("CurrentTile or NewTile is null");
        } 
        return false;
    }

    List<Tile> findQuickestPath(Tile startingTile, Tile destinationTile) {
        if (startingTile == null || destinationTile == null) return null;
        
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
                    // Allow moving through tiles that are either unoccupied OR the destination
                    if (!visitedTiles.Contains(neighbour) && (!neighbour.occupied || neighbour == destinationTile)) {
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