using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// TODO:
// * Add health and attack cooldown to players HUD
// * Stop attack range from showing up on obstacles (or at least look different).
// * Add inventory to store items picked up from enemies. (Which can be used to unlock doors and chests)
// * Add support for additional attacks for player.

// * Add random room gen
// * Add chests that give a random loot pool.

public class Movement 
{
  public Vector3 position;
  public Movement previousMovement;

  public Movement(Vector3 position, Movement previousMovement = null) {
        this.position = position;
        this.previousMovement = previousMovement;
    }
}

public class characterController : MonoBehaviour {
    [SerializeField]
    private float speed = 5;
    [SerializeField]
    private Transform movePoint;
    [SerializeField]
    public tilemapManager tilemapManager;
    [SerializeField]
    private LayerMask obstacleMask;
    [SerializeField]
    public float moveDistance = 1.5f;
    private bool facingLeft = true;
    [SerializeField]
    public GameObject attack;

    bool isTurnEnding;

    private SpriteRenderer spriteRenderer;
    public float flashDuration = 0.1f; 
    private Color originalColor;

    public GameObject directionPoints;
    private GameObject attackObject;
    private GameObject attackRangeObject;
    public int health = 100;
    private Transform nearestPoint = null;
    bool isAiming = false;
    bool canAim = true;
    public int actionsPerMovement = 1;
    public int actionsPerAttack;

    public gameController turnController;
    private Movement currentMovement;
    public bool myTurn = true;
    public Tile currentTile;
    public Vector3Int currentTileInt;
    public Vector3Int startingTile;

    void Start() {
        currentTile = tilemapManager.FindTile(startingTile);
        movePoint.parent = null; 
        currentMovement = new Movement(transform.position, null);
        attack.GetComponent<animationController>().setCurrentCooldown(0);

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void OnValidate()
    {   
        if (tilemapManager == null) return;
        Vector3 parentPos = tilemapManager.gameObject.transform.position;

        float xConst = 0.65f;
        float yConst = 0.9f;
        float zConst = 0f;

        this.transform.position = new Vector3(
            parentPos.x + (startingTile.x * 1.5f) + xConst,
            parentPos.y + (startingTile.y * 1.5f) + yConst,
            parentPos.z + startingTile.z + zConst
        );
    }

    public void takeDamage(int damage)
    {
        // Debug.Log($"Got hit by {damage} damage!");
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
        spriteRenderer.color = Color.red; // Change to red
        yield return new WaitForSeconds(flashDuration); // Wait a tiny bit
        spriteRenderer.color = originalColor; // Return to original
    }

    void die()
    {
        Destroy(this.gameObject);
    }

    public void startTurn() {
        myTurn = true;
        isAiming = false;
        attack.GetComponent<animationController>().decreaseCooldown();
        currentMovement = new Movement(transform.position, null);
    }

    int CustomRound(float value) {
        if (value < 0) {
            if (value % 1 < -0.5f)
                return Mathf.FloorToInt(value);  
            else
                return Mathf.CeilToInt(value);  
        }
        else {
            if (value % 1 >= 0.5f)
                return Mathf.FloorToInt(value);  
            else
                return Mathf.CeilToInt(value);  
        }
    }

    Vector3Int findTilePos(Vector2 pos) {
        int x = CustomRound(pos.x);
        int y = Mathf.FloorToInt(pos.y + 1f);
        return new Vector3Int(x, y, 0);
    }

    Transform findNearestPointPos() {
        GameObject closestPoint = null;  
        float lowestDistance = float.MaxValue;  

        foreach (Transform point in directionPoints.transform) {
            float distance = Vector2.Distance(Camera.main.ScreenToWorldPoint(Input.mousePosition), point.position);

            if (distance < lowestDistance) {
                lowestDistance = distance;
                closestPoint = point.gameObject;
            }
        }

        return closestPoint.transform;
    }

    public void stopAiming(bool decision) {
        isAiming = decision;
        if (!decision && attackRangeObject != null) {
            Destroy(attackRangeObject);
        }
    }

    float Round2F(float point_y) {
        float rounded_y = Mathf.Round(point_y * 2f) / 2f;
        return rounded_y;
    }

    void Update() {
        currentTileInt = currentTile.position;
        if (myTurn) {
            // actionsPerAttack = Mathf.Max(turnController.remainingActions, 1);
            float movementAmount = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, movePoint.position, movementAmount); 

            if (Vector3.Distance(transform.position, movePoint.position) <= 0.05f) {
                if (isTurnEnding)
                {
                    endOfTurn();
                }

                if (Input.GetButtonDown("Horizontal") && !isAiming && !turnController.isTransitioning) {
                    float horizontalInput = Input.GetAxisRaw("Horizontal");

                    if (horizontalInput > 0 && facingLeft) {
                        Flip();
                    }
                    else if (horizontalInput < 0 && !facingLeft) {
                        Flip();
                    }

                    Move(new Vector3(horizontalInput * moveDistance, 0, 0));
                }
                else if (Input.GetButtonDown("Vertical") && !isAiming && !turnController.isTransitioning) {
                    float verticalInput = Input.GetAxisRaw("Vertical");
                    Move(new Vector3(0, verticalInput * moveDistance, 0));
                }
            }

            if (Input.GetButtonDown("Submit") && !turnController.isTransitioning) {
                isTurnEnding = true;
            }

            if (Input.GetButtonDown("Attack") && !turnController.isTransitioning) {
                canAim = true;
            }

            if (Input.GetButton("Attack") && canAim && !turnController.isTransitioning) {
                isAiming = true;
                nearestPoint = findNearestPointPos();

                if (attackRangeObject != null) {
                    Destroy(attackRangeObject);
                }

                GameObject newPrefab = Resources.Load<GameObject>("AttackRanges/" + nearestPoint.name);
                attackRangeObject = Instantiate(newPrefab, new Vector2(nearestPoint.position.x + 0.11f, nearestPoint.position.y + 0.34f ), Quaternion.identity, transform);
            }

            if (Input.GetButtonUp("Attack") && isAiming && !turnController.isTransitioning) {
                if (turnController.canUseAction(actionsPerAttack)) {
                    Attack();
                }
                else {
                    isAiming = false;
                    Debug.Log("Out of Actions");
                }
                Destroy(attackRangeObject);

            }

            if (Input.GetButtonDown("Cancel") && !turnController.isTransitioning) {
                stopAiming(false);
                Destroy(attackRangeObject);
                canAim = false;
            }
        }
    }

    private void endOfTurn() {
        myTurn = false;
        isTurnEnding = false;
        StartCoroutine(deferredEndOfTurn());
    }

    private IEnumerator deferredEndOfTurn() {
        yield return null;
        turnController.endOfTurn();
    }

    void Attack()
    {
        // Debug.Log(attack.GetComponent<animationController>().getCurrentCooldown());
        if (attack.GetComponent<animationController>().getCurrentCooldown() == 0)
        {
            Vector3 direction = (nearestPoint.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 135f;
            if (!facingLeft)
            {
                angle -= 90f;
            }
            Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, angle));
            attackObject = Instantiate(attack, transform.position, Quaternion.identity * rotation, transform);

            turnController.useAction(actionsPerAttack);
            attack.GetComponent<animationController>().startCooldown();
            attackObject.GetComponent<animationController>().isAttacking(true);
            isAiming = false;

            if (turnController.remainingActions == 0)
            {
                endOfTurn();
                Debug.Log("Out of Actions");
            }
        }
        else
        {
            isAiming = false;
            Debug.Log("Attack is on cooldown");
        }
    }

    void setCurrentTile(Tile currentTile, Vector3 direction) {
        currentTile.occupied = false;
        if (direction.x == 0) {
            if (direction.y > 0) {
                this.currentTile = tilemapManager.FindTile(new Vector3Int(currentTile.position.x, currentTile.position.y + 1));
            }
            else {
                this.currentTile = tilemapManager.FindTile(new Vector3Int(currentTile.position.x, currentTile.position.y - 1));
            }
        }
        else if (direction.x > 0) {
            this.currentTile = tilemapManager.FindTile(new Vector3Int(currentTile.position.x + 1, currentTile.position.y));
        }
        else {
            this.currentTile = tilemapManager.FindTile(new Vector3Int(currentTile.position.x - 1, currentTile.position.y));
        }
        this.currentTile.occupied = true;
    }

    bool isNewTileOccupied(Tile currentTile, Vector3 direction) {

        if (currentTile == null) {
            Debug.LogError("currentTile is null in isNewTileOccupied!");
            return true;    
        }
        Tile newTile = null;
        Vector3Int lookupPos = Vector3Int.zero;

        if (direction.x == 0) {
            if (direction.y > 0)
                lookupPos = new Vector3Int(currentTile.position.x, currentTile.position.y + 1);
            else
                lookupPos = new Vector3Int(currentTile.position.x, currentTile.position.y - 1);
        }
        else if (direction.x > 0)
            lookupPos = new Vector3Int(currentTile.position.x + 1, currentTile.position.y);
        else
            lookupPos = new Vector3Int(currentTile.position.x - 1, currentTile.position.y);

        newTile = tilemapManager.FindTile(lookupPos);

        if (newTile == null) {
            Debug.Log($"FindTile returned null!" +
                $"\n  Looking for: {lookupPos}" +
                $"\n  currentTile: {currentTile.position}" +
                $"\n  direction: {direction}" +
                $"\n  tilemapManager: {tilemapManager?.name ?? "NULL"}" +
                $"\n  tileCount: {tilemapManager?.getTilesList().Count ?? -1}" +
                $"\n  First 3 tiles: {string.Join(", ", tilemapManager?.getTilesList().GetRange(0, Mathf.Min(3, tilemapManager.getTilesList().Count)).Select(t => t.position))}");
            return true;
        }

        return newTile.occupied;
    }

    public void ResetMovementHistory()
    {
        currentMovement = new Movement(transform.position, null);
    }

    public void Move(Vector3 direction) {
        Vector3 newPosition = movePoint.position + direction;
        
        if (currentTile == null) {
            Debug.LogError("currentTile is null, cannot move!");
            return;
        }

        if (currentMovement.previousMovement == null || currentMovement.previousMovement.position != newPosition) {
            if (turnController.canUseAction(actionsPerMovement)) {

                if (!isNewTileOccupied(currentTile, direction)) {
                    if (!Physics2D.OverlapCircle(newPosition, 0.2f, obstacleMask)) {
                        Movement newMovement = new Movement(newPosition, currentMovement);
                        currentMovement = newMovement;
                        turnController.useAction(actionsPerMovement);
                        
                        movePoint.position = newPosition;
                        setCurrentTile(currentTile, direction);
                    }
                }
            else {
                Debug.Log("Tile is occupied - cannot move there");
            }
            }
            else {
                endOfTurn();
                Debug.Log("Out of Actions"); 
            }
        }
        else if (myTurn) {
            turnController.giveAction(actionsPerMovement);
            currentMovement = currentMovement.previousMovement;

            if (!Physics2D.OverlapCircle(newPosition, 0.2f, obstacleMask)) {
                movePoint.position = newPosition;
                setCurrentTile(currentTile, direction);
            }
        }
    }

    private void Flip() {
        if (turnController.canUseAction(actionsPerMovement)) {
            facingLeft = !facingLeft;
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }
    }
}
