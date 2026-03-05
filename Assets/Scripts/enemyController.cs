using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json; 
using System.Linq;
using Newtonsoft.Json.Linq;

// TODO
// * Enemies still sometimes fire in the wrong direction
// * Add support for multiple attacks
//  * Give each attack a different cooldown
// * Improve AI so that they take into account if they will get damaged in a given tile by the player. 
//  * If enemy gets hit by another enemy, they will try get out of the way before attacking (all other tiles will get given more points than attacking).
// * Add item dropping capacity (maybe)


public class enemyController : MonoBehaviour {

    public tilemapManager tileManager;
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

    float abilityAngle; 

    private void Start() 
    {
        // Initialize variables from objects in Scene
        movePoint = transform.Find("movePoint").transform;
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

    private void OnValidate()
    {   
        if (transform.parent == null) return;
        Vector3 parentPos = this.gameObject.transform.parent.gameObject.transform.position;

        float xConst = -0.35f;
        float yConst = 2.8f;
        float zConst = 0f;

        this.transform.position = new Vector3(
            parentPos.x + (startingTile.x * 1.5f) + xConst,
            parentPos.y + (startingTile.y * 1.5f) + yConst,
            parentPos.z + startingTile.z + zConst
        );
    }

    public float GetAbilityAngle()
    {
        return abilityAngle;
    }
    
    public void startTurn()
    {
        StartCoroutine(startTurnCourutine());
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
        public int Score { get; set; }
        public int Actions { get; }

        public Option(enemyAbility option, int score)
        {
            AbilityOption = option;
            Score = score;
            Actions = option.actionsRequired;
        }

        public Option(KeyValuePair<Tile, Tile> option, int score)
        {
            MovementOption = option;
            Score = score;
            Actions = 1;
        }

        public Option(Tile option)
        {
            MovementOption = new KeyValuePair<Tile, Tile>(option, option);
            Score = 1;
            Actions = 1;
        }

        public bool isMovement()
        {
            return MovementOption != null;
        }

        public override bool Equals(object obj)
        {
            if (obj is not Option other) return false;
            
            if (isMovement() != other.isMovement()) return false;
            
            if (isMovement())
                return MovementOption.Value.Key == other.MovementOption.Value.Key &&
                    MovementOption.Value.Value == other.MovementOption.Value.Value;
            else
                return AbilityOption == other.AbilityOption;
        }

        public override int GetHashCode()
        {
            if (isMovement())
                return HashCode.Combine(MovementOption.Value.Key, MovementOption.Value.Value);
            else
                return HashCode.Combine(AbilityOption);
        }
    } 

    class Options 
    {
        public List<Option> Values { get; }
        public Options(List<Option> values)
        {
            Values = values;
        }

        public int GetScore()
        {
            int totalScore = 0;
            foreach (Option option in Values)
            {
                totalScore += option.Score;
            }
            return totalScore;
        }

        public int GetTotalActions()
        {
            int totalActions = 0;
            foreach (Option option in Values)
            {
                totalActions += option.Actions;
            }
            return totalActions;
        }

        public Option GetLastMovement(Option currentTile)
        {
            for (int i = Values.Count - 1; i >= 0; i--)
            {
                Option option = Values[i]; 
                if (option.isMovement())
                {
                    return option;
                }
            }
            // Debug.Log("No Movements in `Options`");
            return currentTile;
        }

        public override bool Equals(object obj)
        {
            if (obj is not Options other) return false;
            return Values.SequenceEqual(other.Values);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (Option option in Values)
                hash = hash * 31 + option.GetHashCode();
            return hash;
    }
    }  

    private IEnumerator startTurnCourutine()
    {   
        yield return null;

        // Generate list of total options. (potentially make it a list instead of a dict?)
        Dictionary<int, ActionableOptions> totalOptions = GetTotalOptions(currentTile, numActions, list_of_abilties);
        yield return null;

        // Assign points to all options
        List<Tile> quickestPath = findQuickestPath(currentTile, playerController.currentTile);
        List<ScoredOptions> scoredTotalOptions = AssignMovementPoints(totalOptions, quickestPath);
        yield return null;
        // ViewScoring(scoredTotalOptions);

        // Find best combination of options
        List<Options> totalOptionPermutations = GetTotalOptionPermutations(scoredTotalOptions);
        yield return null;
        // ViewPermutations(totalOptionPermutations);

        List<Options> bestOptions = GetHighestScoreOptionOrder(totalOptionPermutations);
        yield return null;
        // ViewBestOptions(bestOptions);

        // Execute moves/abilities
        int index = UnityEngine.Random.Range(0, bestOptions.Count);
        yield return StartCoroutine(executeActions(bestOptions[index]));

        // End turn
        turnController.endOfTurn();    
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

    private IEnumerator executeActions(Options options)
    {
        foreach (Option option in options.Values)
        {
            if (option.isMovement())
            {
                // Debug.Log("Moving from: " + option.MovementOption.Value.Key.position + " to: " + option.MovementOption.Value.Value.position);
                yield return StartCoroutine(moveEnemyRoutine(option.MovementOption.Value.Value));
            }
            else
            {
                Attack(option.AbilityOption, option.AbilityOption.damage);
                yield return new WaitForSeconds(0.4f);
            }
        }
    }

    private List<ScoredOptions> AssignMovementPoints(Dictionary<int, ActionableOptions> totalOptions, List<Tile> quickestPath)
    {
        List<ScoredOptions> totalScoredOptions = new List<ScoredOptions>();
        Tile playerTile = playerController.currentTile;
        foreach (ActionableOptions options in totalOptions.Values)
        {
            // Assign movement points
            Dictionary<Tile, List<Tile>> movements = options.Movements;

            Dictionary<KeyValuePair<Tile, Tile>, int> scoredMovements = new Dictionary<KeyValuePair<Tile, Tile>, int>();
            foreach (var keyPair in movements)
            {
                Tile startingTile = keyPair.Key;
                List<Tile> endingTileList = keyPair.Value;

                foreach (Tile tile in endingTileList)
                {
                    KeyValuePair<Tile, Tile> movement = new KeyValuePair<Tile, Tile>(startingTile, tile);
                    if (quickestPath.Contains(tile) && tile != playerTile) {
                        scoredMovements[movement] = quickestPath.IndexOf(tile) + 1;
                    }
                    else
                    {
                        scoredMovements[movement] = 0;
                    }
                }
            }

            // Assign placeholder ability points
            List<enemyAbility> abilities = options.Abilities;

            Dictionary<enemyAbility, int> scoredAbilities = new Dictionary<enemyAbility, int>();
            foreach (enemyAbility ability in abilities)
            {
                 scoredAbilities[ability] = 0;
            }

            totalScoredOptions.Add(new ScoredOptions(scoredMovements, scoredAbilities));
        }
        return totalScoredOptions;
    }

    private List<Options> GetHighestScoreOptionOrder(List<Options> totalPermutations)
    {
        List<Options> highestScoreOptions = new List<Options>();
        int highestScore = 0;
        foreach (Options permutation in totalPermutations)
        {
            int currentScore = permutation.GetScore();

            if (currentScore > highestScore)
            {
                highestScoreOptions = new List<Options>{permutation};
                highestScore = currentScore;
            }
            else if (currentScore == highestScore)
            {
                highestScoreOptions.Add(permutation);
            }
        }

        foreach (Options options in highestScoreOptions)
        {
            options.Values.RemoveAll(option => 
                option.isMovement() && 
                option.MovementOption.Value.Key == option.MovementOption.Value.Value
            );
        }

        return highestScoreOptions;
    }

    private List<Options> GetTotalOptionPermutations(List<ScoredOptions> options)
    {
        List<Options> permutations = new List<Options>();

        for (int i = 0; i < options.Count; i++)
        {
            ScoredOptions iterationScoredOptions = options[i];

            // Convert `Movements` to `Options`
            List<Option> iterationOptions = new List<Option>();
            foreach (var pair in iterationScoredOptions.Movements)
            {
                iterationOptions.Add(new Option(pair.Key, pair.Value));
            }
            // Convert `Abilities` to `Options`
            foreach (var pair in iterationScoredOptions.Abilities)
            {
                iterationOptions.Add(new Option(pair.Key, pair.Value));
            }

            // For first iteration of movement
            if (i == 0)
            {
                foreach (Option option in iterationOptions)
                {
                    Option _option = option;
                    if (_option.Actions <= numActions)
                    {
                        if (!option.isMovement())
                        {
                            _option = SetAbilityScore(_option);
                        }
                        permutations.Add(new Options(new List<Option> { _option }));
                    }
                }
            }
            // For remaining iterations of movement
            else
            {
                List<Options> newPermutations = new List<Options>();

                foreach (Options permutation in permutations)
                {
                    Option lastOption = permutation.Values[permutation.Values.Count - 1];
                    
                    foreach (Option option in iterationOptions)
                    {
                        if (!lastOption.isMovement())
                        {
                            lastOption = permutation.GetLastMovement(new Option(currentTile));
                        }

                        if (option.isMovement()) {
                            Tile startingTile = option.MovementOption.Value.Key;
                            Tile endingTile = lastOption.MovementOption.Value.Value;

                            if (endingTile == startingTile)
                            {
                                List<Option> newPermutation = new List<Option>(permutation.Values);
                                newPermutation.Add(option);
                                newPermutations.Add(new Options(newPermutation));
                            }
                        }
                        else
                        {
                            Option scoredOption = SetAbilityScore(option, lastOption.MovementOption.Value.Value);
                            if ((permutation.GetTotalActions() + scoredOption.Actions) <= numActions) {
                                List<Option> newPermutation = new List<Option>(permutation.Values);
                                newPermutation.Add(scoredOption);
                                newPermutations.Add(new Options(newPermutation));
                            }
                            else
                            {
                                List<Option> newPermutation = new List<Option>(permutation.Values);
                                Options newPermutationOptions = new Options(newPermutation);

                                if (!newPermutations.Contains(newPermutationOptions)) {
                                    newPermutations.Add(newPermutationOptions);
                                }
                            } 
                        }
                    }
                }

                permutations = newPermutations;
            } 
        }
        return permutations;
    }

    public void takeDamage(int damage)
    {
        // Debug.Log($"Dealt {damage} damage!");
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
        // Debug.Log($"Use {ability.name} for {damageAmount} damage.");
        currentActions = currentActions - ability.actionsRequired;
        
        Vector3 direction = (playerObject.transform.position - transform.position).normalized;
        abilityAngle = Vector2.SignedAngle(Vector2.up, direction) + 180f;
        Debug.Log(abilityAngle);


        Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, abilityAngle));

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

    private bool isSightObstructed(Tile enemyTile, Tile playerTile)
    {        
        int startX = enemyTile.position.x;
        int endX = playerTile.position.x;
        int startY = enemyTile.position.y;
        int endY = playerTile.position.y;

        if (startY == endY)
        {
            int step = (endX > startX) ? 1 : -1;
            for (int x = startX + step; x != endX; x += step)
            {
                Tile tile = tileManager.FindTile(new Vector3Int(x, startY, 0));
                if (tile != null && tile.occupied) return true;
            }
        }
        else if (startX == endX)
        {
            int step = (endY > startY) ? 1 : -1;
            for (int y = startY + step; y != endY; y += step)
            {
                Tile tile = tileManager.FindTile(new Vector3Int(startX, y, 0));
                if (tile != null && tile.occupied) return true;
            }
        }

        return false;
    }

    private Option SetAbilityScore(Option option)
    {
        return SetAbilityScore(option, currentTile);
    }
    private Option SetAbilityScore(Option option, Tile currentTile)
    {
         // Get ability attributes
        enemyAbility ability = option.AbilityOption;
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
                    if (!isSightObstructed(currentTile, playerTile)) {
                        playerInRange = true;
                        break;
                    }
                }
            }
        }
        
        // Assign points to ability if player is in range
        int score = 0;
        if (playerInRange)
        {
            score = damage;
        }
        return new Option(ability, score);
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

    private void ViewBestOptions(List<Options> bestOptions)
    {
        Debug.Log("LIST OF BEST OPTIONS WITH A SCORE OF: " + bestOptions[0].GetScore() + ":");
        for (int i = 0; i < bestOptions.Count; i++) 
        {
            var options = bestOptions[i];
            Debug.Log("Option #" + (i+1));
            foreach(Option option in options.Values)
            {
                if (option.isMovement())
                {
                    Debug.Log("Move from " + option.MovementOption.Value.Key.position + " to " + option.MovementOption.Value.Value.position + ". Score: " + option.Score);
                }
                else
                {
                    Debug.Log("Use " + option.AbilityOption.name + ". Score: " + option.Score);
                }
            }
        }
    }
    private void ViewPermutations(List<Options> permutations)
    {
        Debug.Log("SEPERATE ITERATION");
        Debug.Log("\n_________________________________________\n");
        foreach (Options perm in permutations)
        {
            Debug.Log("SEPERATE PERMUTATION, Score: " + perm.GetScore());
            foreach (Option o in perm.Values)
            {
                if (o.isMovement())
                {
                    Debug.Log("Move from " + o.MovementOption.Value.Key.position + " to " + o.MovementOption.Value.Value.position + ". Score: " + o.Score);
                }
                else
                {
                    Debug.Log("Use " + o.AbilityOption.name + ". Score: " + o.Score);
                }
            }
        }
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