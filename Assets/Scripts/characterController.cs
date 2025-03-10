
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    public GameObject directionPoints;
    private GameObject attackObject;
    private GameObject attackRangeObject;
    private Transform nearestPoint = null;
    bool isAiming = false;
    bool canAim = true;
    public int actionsPerMovement = 1;
    public int actionsPerAttack;

    public gameController turnController;
    private Movement currentMovement;
    private bool myTurn = true;
    public Tile currentTile;

    void Start() {
        currentTile = tilemapManager.FindTile(new Vector3Int(-3, -4, 0));
        movePoint.parent = null; 
        currentMovement = new Movement(transform.position, null);
    }

    public void startTurn() {
        myTurn = true;
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
        actionsPerAttack = Mathf.Max(turnController.remainingActions, 1);
        float movementAmount = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, movePoint.position, movementAmount);

        if (Vector3.Distance(transform.position, movePoint.position) <= 0.05f) {
            if (Input.GetButtonDown("Horizontal") && !isAiming) {
                float horizontalInput = Input.GetAxisRaw("Horizontal");

                if (horizontalInput > 0 && facingLeft) {
                    Flip();
                }
                else if (horizontalInput < 0 && !facingLeft) {
                    Flip();
                }

                Move(new Vector3(horizontalInput * moveDistance, 0, 0));
            }
            else if (Input.GetButtonDown("Vertical") && !isAiming) {
                float verticalInput = Input.GetAxisRaw("Vertical");
                Move(new Vector3(0, verticalInput * moveDistance, 0));
            }
        }

        if (Input.GetButtonDown("Submit")) {
            endOfTurn();
        }

        if (Input.GetButtonDown("Attack")) {
            canAim = true;
        }

        if (Input.GetButton("Attack") && canAim) {
            isAiming = true;
            nearestPoint = findNearestPointPos();

            if (attackRangeObject != null) {
                Destroy(attackRangeObject);
            }

            GameObject newPrefab = Resources.Load<GameObject>("AttackRanges/" + nearestPoint.name);
            attackRangeObject = Instantiate(newPrefab, new Vector2(nearestPoint.position.x, Round2F(nearestPoint.position.y)), Quaternion.identity, transform);
        }

        if (Input.GetButtonUp("Attack") && isAiming) {
            if (turnController.canUseAction(actionsPerAttack)) {
                Vector3 direction = (nearestPoint.position - transform.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 135f;
                if (!facingLeft) {
                    angle -= 90f;
                }
                Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, angle));
                attackObject = Instantiate(attack, transform.position, Quaternion.identity * rotation, transform);

                turnController.useAction(actionsPerAttack);
                attackObject.GetComponent<animationController>().isAttacking(true);
                isAiming = false;

                if (turnController.remainingActions == 0) {
                    //endOfTurn();
                    Debug.Log("Out of Actions");
                }
            }
            else {
                // endOfTurn();
                Debug.Log("Out of Actions");
            } 
            Destroy(attackRangeObject);

        }

        if (Input.GetButtonDown("Cancel")) {
            stopAiming(false);
            Destroy(attackRangeObject);
            canAim = false;
        }
    }

    void setCurrentTile(Tile currentTile, Vector3 direction) {
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
    }

    bool isNewTileOccupied(Tile currentTile, Vector3 direction) {
        Tile newTile;
        if (direction.x == 0) {
            if (direction.y > 0) {
                newTile = tilemapManager.FindTile(new Vector3Int(currentTile.position.x, currentTile.position.y + 1));
            }
            else {
                newTile = tilemapManager.FindTile(new Vector3Int(currentTile.position.x, currentTile.position.y - 1));
            }
        }
        else if (direction.x > 0) {
            newTile = tilemapManager.FindTile(new Vector3Int(currentTile.position.x + 1, currentTile.position.y));
        }
        else {
            newTile = tilemapManager.FindTile(new Vector3Int(currentTile.position.x - 1, currentTile.position.y));
        }

        return newTile.occupied;
    }

    private void Move(Vector3 direction) {
        Vector3 newPosition = movePoint.position + direction;
        
        if (currentMovement.previousMovement == null || currentMovement.previousMovement.position != newPosition) {
            if (turnController.canUseAction(actionsPerMovement)) {

                if (!isNewTileOccupied(currentTile, direction)) {
                    Movement newMovement = new Movement(newPosition, currentMovement);
                    currentMovement = newMovement;
                    turnController.useAction(actionsPerMovement);
                }

                if (!Physics2D.OverlapCircle(newPosition, 0.2f, obstacleMask)) {
                    movePoint.position = newPosition;
                    setCurrentTile(currentTile, direction);
                }
            }
            else {
                // endOfTurn();
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
    
    private void endOfTurn() {
        myTurn = false;
        turnController.endOfTurn();
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
