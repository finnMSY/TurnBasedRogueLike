using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json; 
using System.Linq;
using Newtonsoft.Json.Linq;

// TODO
// * Enemies still sometimes fire in the wrong direction
// * Improve enemy AI so that they can move and attack on the same move
// * Add support for multiple attacks
//  * Give each attack a different cooldown
//
// * Improve AI so that they take into account if they will get damaged in a given tile by the player. 
// * If enemy gets hit by another enemy, they will try get out of the way before attacking (all other tiles will get given more points than attacking).
// * Allow attacks to be stored on the enemy rather then on the turnController (a custmom dropdown that contains all of the given spells for that particular enemy class / level).
// * Add item drops


public class enemyController : MonoBehaviour {

    private tilemapManager tileManager;
    public int numActions;
    private int currentActions;
    private SpriteRenderer spriteRenderer;
    public float flashDuration = 0.1f; 
    public GameObject deathAnim;
    private Color originalColor;
    private List<Tile> totalTiles = new List<Tile>();
    private characterController playerController;
    private GameObject attackObject;
    private gameController turnController;
    // private Dictionary<Tile, int> tileActionPoints;
    // private Dictionary<Ability, int> abilityActionPoints;
    public EnemyType enemyType;
    public List<GameObject> abilities; 
    private GameObject playerObject;
    private List<enemyAbility> list_of_abilties;
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
        // Initialize variables from objects in Scene
        movePoint = transform.Find("movePoint").transform;
        tileManager = FindObjectOfType<tilemapManager>();
        turnController = FindObjectOfType<gameController>();
        playerObject = GameObject.FindGameObjectWithTag("Player");
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        // Initialize characterController script
        playerController = playerObject.GetComponent<characterController>();
        if (playerController == null)
        {
            Debug.LogError("Character Controller Script does not exist on Player object.");
            return;
        }

        // Get the enemies list of abilities
        list_of_abilties = new List<enemyAbility>();
        foreach (GameObject ability in abilities)
        {
            enemyAbility abilityObject = ability.GetComponent<enemyAbility>();
            list_of_abilties.Add(abilityObject);
        }

        // Detatch movePoint from GameObject
        movePoint.parent = null;  

        // Find starting tile
        this.currentTile = tileManager.FindTile(startingTile);
        
        // Assign current tile as 'occupied' 
        if (currentTile != null)
        {
            currentTile.occupied = true;
        }
    }
    
    public void startTurn()
    {
        StartCoroutine(startTurnCourutine());
    }

    private IEnumerator startTurnCourutine()
    {   
        currentActions = numActions;
        while (currentActions > 0) {
            // Define score dictionaries
            Dictionary<Tile, int> points_per_tile = new Dictionary<Tile, int>();
            Dictionary<enemyAbility, int> points_per_ability = new Dictionary<enemyAbility, int>();

            // Gets the quickest path
            List<Tile> quickestPath = findQuickestPath(currentTile, playerController.currentTile);

            // Finds all moveable tiles
            List<Tile> totalMoveableTiles = findTotalMoveableTiles(currentTile, currentActions);

            // Assigns points to all tiles and attack
            (points_per_tile, points_per_ability) = assignPoints(quickestPath, totalMoveableTiles, points_per_tile, points_per_ability);
            
            // Wait for movement/attack to complete
            yield return StartCoroutine(attackOrMoveCoroutine(points_per_tile, points_per_ability));
        }
        
        // End turn
        turnController.endOfTurn();
    }

    private IEnumerator attackOrMoveCoroutine(Dictionary<Tile, int> points_per_tile,  Dictionary<enemyAbility, int> points_per_ability)
    {
        // Get the tile highest points
        (Tile maxTile, int maxTilePoints) = getHighestTile(points_per_tile);
        if (maxTile == null)
        {
            maxTilePoints = 0;
        }

        // Get the ability with the highest points
        (enemyAbility maxAbility, int maxAbilityPoints) = getHighestAbility(points_per_ability);
        if (maxAbility == null)
        {
            maxAbilityPoints = 0;
        }    

        if (maxTilePoints == 0 && maxAbilityPoints == 0)
        {
            Debug.Log("No valid moves or attacks");
            currentActions = 0;
            yield break;
        }

        Debug.Log(maxAbilityPoints + " + " + maxTilePoints);
                
        // Execute the move action of the max tile points in higher than the max ability points and execute the ability or not 
        if (maxTilePoints > maxAbilityPoints)
        {
            Debug.Log("Moving from: " + currentTile.position + " to: " + maxTile.position);
            yield return StartCoroutine(moveEnemyRoutine(maxTile));
        }
        else
        {
            Attack(maxAbility, maxAbilityPoints);
            yield return new WaitForSeconds(0.4f);
        }
    }

    (Dictionary<Tile, int>? points_per_tile, Dictionary<enemyAbility, int>? points_per_ability) assignPoints(List<Tile> quickestPath, List<Tile> totalMoveableTiles, Dictionary<Tile, int> points_per_tile, Dictionary<enemyAbility, int> points_per_ability) 
    {
        if (quickestPath == null) return (null, null);
        
        // Assign points to all tiles in the quickest path that are reachable with current actions
        if (quickestPath.Count >= 2)
        {
            Tile nextStepTile = quickestPath[1];
            if (!points_per_tile.ContainsKey(nextStepTile))
            {
                // Give it high points to prefer moving toward player
                points_per_tile.Add(nextStepTile, 1);
            }
        }

        // Get the players current tile
        Tile playerTile = playerController.currentTile;

        // Assign points to each ability
        foreach (enemyAbility ability in list_of_abilties)
        {
            if (ability.actionsRequired > currentActions)
            {
                continue;
            }

            // Get ability attributes
            int damage = ability.damage;
            Dictionary<string, object> range = JsonConvert.DeserializeObject<Dictionary<string, object>>(ability.range.text);
            JArray range_tiles = JArray.Parse(range["tiles"].ToString());
            
            // Check if the player is within the abilities range from their current position
            bool playerInRange = false;
            foreach (JObject tile in range_tiles)
            {
                // Get relative coordinates of tile
                int x = (int)tile["x"];
                int y = (int)tile["y"];
                
                // Convert relative coordinates to world position
                Vector3Int targetPos = new Vector3Int(
                    currentTile.position.x + x,
                    currentTile.position.y + y,
                    0
                );
                
                // Find in-game tile at those coordinates
                Tile targetTile = tileManager.FindTile(targetPos);
                if (targetTile != null) 
                {                    
                    if (targetTile == playerTile)
                    {
                        playerInRange = true;
                        Debug.Log("PLAYER IN RANGE!");
                        break;
                    }
                }
                else
                {
                    // Debug.Log($"No tile found at position: {targetPos} (out of bounds)");
                }
            }
            
            // Assign points to ability if player is in range
            if (playerInRange)
            {
                if (points_per_ability.ContainsKey(ability))
                {
                    points_per_ability[ability] = Mathf.Max(points_per_ability[ability], damage);
                }
                else
                {
                    points_per_ability.Add(ability, damage);
                }
            }
        }

        // Assign the remaining moveable tiles 0 points
        if (totalMoveableTiles != null)
        {
            foreach(Tile t in totalMoveableTiles) {
                if (!points_per_tile.ContainsKey(t)) {
                    points_per_tile.Add(t, 0);
                }
            }
        }

        return (points_per_tile, points_per_ability);
    }

    public (Tile? tile, int points) getHighestTile(Dictionary<Tile, int> points_per_tile)
    {
        if (points_per_tile.Count == 0) 
            return (null, 0);
        
        var maxPair = points_per_tile.OrderByDescending(x => x.Value).First();
        return (maxPair.Key, maxPair.Value);
    }

    public (enemyAbility? ability, int points) getHighestAbility(Dictionary<enemyAbility, int> points_per_ability)
    {
        if (points_per_ability.Count == 0) 
            return (null, 0);
        
        var maxPair = points_per_ability.OrderByDescending(x => x.Value).First();
        return (maxPair.Key, maxPair.Value);
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
            StartCoroutine(flashRed());
        }
    }

    IEnumerator flashRed() {
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
        Instantiate(deathAnim, transform.position, Quaternion.identity);
    }

    private void Attack(enemyAbility ability, int damageAmount)
    {
        Debug.Log($"Use {ability.name} for {damageAmount} damage.");
        currentActions = currentActions - ability.actionsRequired;
        
        Vector3 direction = (playerObject.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        attackObject = Instantiate(ability.gameObject, transform.position, Quaternion.identity * rotation, transform);
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

     IEnumerator moveEnemyRoutine(Tile tile)
    {
       
        moveEnemy(tile);
        yield return new WaitForSeconds(.4f);   
        
    }

    void moveEnemy(Tile nextTile)
    {
        // if (tileActionPoints == null || tileActionPoints.Count == 0) return;
        // // Find the tile with highest score anywhere
        // Tile targetTile = tileActionPoints.OrderByDescending(kv => kv.Value).First().Key;
        // if (targetTile == currentTile) return; // Already on the best tile
        // // Find quickest path to that tile
        // List<Tile> path = findQuickestPath(currentTile, targetTile);
        // if (path == null || path.Count < 2) return; // No movement possible
        // Tile nextTile = path[1]; // The next step along the path

        // Check if next tile is occupied
        if (nextTile.occupied && nextTile != currentTile)
        {
            Debug.Log($"Enemy {gameObject.name} can't move - tile occupied");
            currentActions = currentActions - 1;
            return;
        }

        bool didEnemyMove = moveGameObject(currentTile, nextTile);

        // Free previous tile and occupy new tile if enemy did move
        if (didEnemyMove)
        {
            currentActions = currentActions - 1;

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