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
        // StartCoroutine(startTurnCourutine());
        startTurnCourutine();
    }

    class ActionableOptions 
    {
        public Dictionary<Tile, List<Tile>> Movements { get; }
        public List<enemyAbility> Abilities { get; }

        public ActionableOptions(Dictionary<Tile, List<Tile>> movements, List<enemyAbility> abilities)
        {
            Movements = movements;
            Abilities = abilities;
        }
    }   

    class ScoredOptions 
    {
        public Dictionary<KeyValuePair<Tile, Tile>, int> Movements { get; }
        public Dictionary<enemyAbility, int> Abilities { get; }

        public ScoredOptions(Dictionary<KeyValuePair<Tile, Tile>, int> movements, Dictionary<enemyAbility, int> abilities)
        {
            Movements = movements;
            Abilities = abilities;
        }
    }  

    class Option 
    {
        public KeyValuePair<Tile, Tile>? MovementOption { get; }
        public enemyAbility AbilityOption { get; }
        public int Score { get; }

        public Option(enemyAbility option, int score)
        {
            AbilityOption = option;
            Score = score;
        }

        public Option(KeyValuePair<Tile, Tile> option, int score)
        {
            MovementOption = option;
            Score = score;
        }

        public bool isMovement()
        {
            return MovementOption != null;
        }
    } 

    // class Options 
    // {
    //     public Option(enemyAbility option, int score)
    //     {
    //         AbilityOption = option;
    //         Score = score;
    //     }

    // }  

    private void startTurnCourutine()
    {   
        // Generate list of total options. (potentially make it a list instead of a dict?)
        Dictionary<int, ActionableOptions> totalOptions = GetTotalOptions(currentTile, numActions, list_of_abilties);

        // Assign points to all options
        List<Tile> quickestPath = findQuickestPath(currentTile, playerController.currentTile);
        List<ScoredOptions> scoredTotalOptions = AssignPoints(totalOptions, quickestPath);
        // ViewScoring(scoredTotalOptions);

        // Find best combination of options
        List<ScoredOptions> optionOrder = GetHighestScoreOptionOrder(scoredTotalOptions);

        // Execute moves/abilities
        //...

        // End turn
        turnController.endOfTurn();    


















        // for (int i = 0; i < numActions; i++)
        // {
        //     List<Tile> totalMoveableTiles = 

        //     foreach (Tile t in ts)
        //     {
        //         List<Tile> totalMoveableTiles = findTotalMoveableTiles(t, currentActions);
        //     } 
            
        //     List<enemyAbility> totalUseableAbilities = findTotalUseableAbilities(list_of_abilties, currentActions);
        //     totalOptions[i] = ActionableOptions(totalMoveableTiles, totalUseableAbilities);
        // }
        
        // Calculate points to all options

        // Determine based combination of points 




        // currentActions = numActions;
        // while (currentActions > 0) {
        //     // Define score dictionaries
        //     Dictionary<Tile, int> points_per_tile = new Dictionary<Tile, int>();
        //     Dictionary<enemyAbility, int> points_per_ability = new Dictionary<enemyAbility, int>();

        //     // Gets the quickest path
        //     List<Tile> quickestPath = findQuickestPath(currentTile, playerController.currentTile);

        //     // Finds all moveable tiles
        //     List<Tile> totalMoveableTiles = findTotalMoveableTiles(currentTile, currentActions);

        //     // Assigns points to all tiles and attack
        //     (points_per_tile, points_per_ability) = assignPoints(quickestPath, totalMoveableTiles, points_per_tile, points_per_ability);
            
        //     // Wait for movement/attack to complete
        //     yield return StartCoroutine(attackOrMoveCoroutine(points_per_tile, points_per_ability));
        // }
    }

    private Dictionary<int, ActionableOptions> GetTotalOptions(Tile currentTile, int numActions, List<enemyAbility> listOfAbilties)
    {
        var totalOptions = new Dictionary<int, ActionableOptions>();
        Queue<Tile> tileQueue = new Queue<Tile>();
        tileQueue.Enqueue(currentTile);

        for (int i = 0; i < numActions; i++) {
            Dictionary<Tile, List<Tile>> movements = new Dictionary<Tile, List<Tile>>();
            List<Tile> totalTiles = new List<Tile>();

            while (tileQueue.Count > 0)
            {
                Tile tile = tileQueue.Dequeue();
                List<Tile> totalMoveableTiles = FindTotalMoveableTiles(tile, 1);
                totalTiles.AddRange(totalMoveableTiles);

                movements[tile] = totalMoveableTiles;
            }

            foreach (Tile tile in totalTiles)
            {
                tileQueue.Enqueue(tile);  
            }

            List<enemyAbility> totalUseableAbilities = FindTotalUseableAbilities(listOfAbilties, numActions-i);
            totalOptions[i] = new ActionableOptions(movements, totalUseableAbilities);
        } 

        return totalOptions;
    }

    private List<ScoredOptions> AssignPoints(Dictionary<int, ActionableOptions> totalOptions, List<Tile> quickestPath)
    {
        List<ScoredOptions> totalScoredOptions = new List<ScoredOptions>();
        foreach (ActionableOptions options in totalOptions.Values)
        {
            // Assign movement points
            Dictionary<Tile, List<Tile>> movements = options.Movements;

            Dictionary<KeyValuePair<Tile, Tile>, int> scoredMovements = new Dictionary<KeyValuePair<Tile, Tile>, int>();
            foreach (var movement in movements)
            {
                var from = movement.Key;
                var toList = movement.Value;

                var list = new List<KeyValuePair<Tile, Tile>>();
                foreach (Tile t in toList)
                {
                    var keyPair = new KeyValuePair<Tile, Tile>(from, t);

                    if (t == currentTile)
                    {
                        scoredMovements[keyPair] = 1;
                    }
                    else if (quickestPath.Contains(t)) {
                        scoredMovements[keyPair] = 2;
                    }
                    else
                    {
                        scoredMovements[keyPair] = 0;
                    }
                }
            }

            // Assign ability points
            List<enemyAbility> abilities = options.Abilities;

            Dictionary<enemyAbility, int> scoredAbilities = new Dictionary<enemyAbility, int>();
            foreach (enemyAbility ability in abilities)
            {
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
                    Tile playerTile = playerController.currentTile;
                    if (targetTile != null) 
                    {                    
                        if (targetTile == playerTile)
                        {
                            playerInRange = true;
                            break;
                        }
                    }
                }
                
                // Assign points to ability if player is in range
                if (playerInRange)
                {
                    scoredAbilities[ability] = damage;
                }
                else
                {
                    scoredAbilities[ability] = 0;
                }
            }

            totalScoredOptions.Add(new ScoredOptions(scoredMovements, scoredAbilities));
        }
        return totalScoredOptions;
    }

    private List<ScoredOptions> GetHighestScoreOptionOrder(List<ScoredOptions> options)
    {
        List<List<Option>> permutations = new List<List<Option>>();

        for (int i = 0; i < options.Count; i++)
        {
            ScoredOptions iterationScoredOptions = options[i];

            // Convert `Movements` to `Options`
            List<Option> iterationOptions = new List<Option>();
            foreach (var pair in iterationScoredOptions.Movements)
            {
                iterationOptions.Add(new Option(pair.Key, pair.Value));
            }

            // For first iteration of movements
            if (i == 0)
            {
                foreach (Option option in iterationOptions)
                {
                    permutations.Add(new List<Option> { option });
                }
            }
            else
            {
                List<List<Option>> newPermutations = new List<List<Option>>();

                foreach (List<Option> permutation in permutations)
                {
                    Option lastOption = permutation[permutation.Count - 1];
                    Tile endingTile = lastOption.MovementOption.Value.Value;

                    foreach (Option option in iterationOptions)
                    {
                        Tile startingTile = option.MovementOption.Value.Key;
                        if (endingTile == startingTile)
                        {
                            List<Option> newPermutation = new List<Option>(permutation);
                            newPermutation.Add(option);
                            newPermutations.Add(newPermutation);
                        }
                    }
                }

                permutations = newPermutations;
            } 
        }

        Debug.Log("SEPERATE ITERATION");
        Debug.Log("\n_________________________________________\n");
        foreach (List<Option> perm in permutations)
        {
            Debug.Log("SEPERATE PERMUTATION");
            foreach (Option o in perm)
            {
                if (o.isMovement())
                {
                    Debug.Log("Move from " + o.MovementOption.Value.Key.position + " to " + o.MovementOption.Value.Value.position + ". Score");
                }
                else
                {
                    Debug.Log("Use " + o.AbilityOption.name);
                }
            }
        }

        return options;
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


    List<enemyAbility> FindTotalUseableAbilities(List<enemyAbility> abilties, int actions)
    {
        List<enemyAbility> useableAbilities = new List<enemyAbility>();
        foreach(enemyAbility ability in abilties)
        {
            if (ability.actionsRequired <= actions)
            {
                useableAbilities.Add(ability);
            }
        }
        return useableAbilities;
    }

    List<Tile> FindTotalMoveableTiles(Tile startingTile, int maxLevels) {
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

    private void ViewScoring(List<ScoredOptions> scoredTotalOptions)
    {
        foreach(ScoredOptions so in scoredTotalOptions)
        {
            Debug.Log("Action TURN");
            Debug.Log("\n___________________________________________\n");
            foreach (var pair in so.Movements)
            {
                var price = pair.Value;
                var m = pair.Key;
                Debug.Log("Moving from " + m.Key.position + " to " + m.Value.position + " has a score of: " + price);
            }
            Debug.Log("\n___________________________________________\n");
            foreach (var pair in so.Abilities)
            {
                var ability = pair.Key;
                var price = pair.Value;
                Debug.Log("Using " + ability.name + " has a score of: " + price);
            }
            if (so.Abilities.Count == 0)
            {
                Debug.Log("You cannot use any abilities.");
            }
            Debug.Log("\n___________________________________________\n\n\n");
        }
    }

    void Update() {
        float movementAmount = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, movePoint.position, movementAmount);
    }
}