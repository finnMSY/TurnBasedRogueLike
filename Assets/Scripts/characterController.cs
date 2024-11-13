using UnityEngine;
using System.Collections;

public class characterController : MonoBehaviour {
    [SerializeField]
    private float speed = 5;
    [SerializeField]
    private Transform movePoint;
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


    void Start() {
        movePoint.parent = null; 
    }

    Transform findNearestPointPos()
    {
        GameObject closestPoint = null;  
        float lowestDistance = float.MaxValue;  

        foreach (Transform point in directionPoints.transform) 
        {
            float distance = Vector2.Distance(Camera.main.ScreenToWorldPoint(Input.mousePosition), point.position);

            if (distance < lowestDistance) 
            {
                lowestDistance = distance;
                closestPoint = point.gameObject;
            }
        }

        return closestPoint.transform;
    }

    void Update() {
        float movementAmount = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, movePoint.position, movementAmount);

        if (Vector3.Distance(transform.position, movePoint.position) <= 0.05f) {
            if (Input.GetButtonDown("Horizontal")) {
                float horizontalInput = Input.GetAxisRaw("Horizontal");

                if (horizontalInput > 0 && facingLeft) {
                    Flip();
                }
                else if (horizontalInput < 0 && !facingLeft) {
                    Flip();
                }

                Move(new Vector3(horizontalInput * moveDistance, 0, 0));
            }
            else if (Input.GetButtonDown("Vertical")) {
                float verticalInput = Input.GetAxisRaw("Vertical");
                Move(new Vector3(0, verticalInput * moveDistance, 0));
            }
        }

        Transform nearestPoint = null;

        if (Input.GetButtonDown("Attack")) {
            attack.GetComponent<animationController>().isAttacking(false);
            
            nearestPoint = findNearestPointPos();
            Vector3 direction = (nearestPoint.position - transform.position).normalized;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 135f;
            if (!facingLeft) {
                angle -= 90f;
            }
            Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, angle));
            attackObject = Instantiate(attack, transform.position, Quaternion.identity * rotation, transform);

            GameObject prefab = Resources.Load<GameObject>("AttackRanges/" + nearestPoint.name);
            attackRangeObject = Instantiate(prefab, nearestPoint.position, Quaternion.identity, transform);
        }

        if (Input.GetButton("Attack")) {
            Transform newNearestPoint = findNearestPointPos();

            if (newNearestPoint != nearestPoint) {
                nearestPoint = newNearestPoint;

                if (attackRangeObject != null) {
                    Destroy(attackRangeObject);
                }

                GameObject newPrefab = Resources.Load<GameObject>("AttackRanges/" + nearestPoint.name);
                attackRangeObject = Instantiate(newPrefab, nearestPoint.position, Quaternion.identity, transform);
            }
        }

        if (Input.GetButtonUp("Attack")) {
            Destroy(attackRangeObject);
            attackObject.GetComponent<animationController>().isAttacking(true);
        }
    }



    private void Move(Vector3 direction) {
        Vector3 newPosition = movePoint.position + direction;
        if (!Physics2D.OverlapCircle(newPosition, 0.2f, obstacleMask)) {
            movePoint.position = newPosition;
        }
    }

    private void Flip() {
        facingLeft = !facingLeft;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }
}
