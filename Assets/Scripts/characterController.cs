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

    void Start() {
        movePoint.parent = null; 
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

        if (Input.GetButtonUp("Attack")) {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0; // Set z to zero if working in 2D, adjust as needed for 3D

            // Calculate the direction from the player to the mouse
            Vector3 direction = (mousePos - transform.position).normalized;

            // Calculate the angle and add 90 degrees offset
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 135f;
            if (!facingLeft) {
                angle -= 90f;
            }
            Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, angle));
            Instantiate(attack, transform.position, Quaternion.identity * rotation, transform);
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
