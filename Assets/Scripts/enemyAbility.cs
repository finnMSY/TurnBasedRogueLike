using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyAbility : MonoBehaviour
{
    public string name;
    public int damage;
    public TextAsset range;
    public AnimationClip animation;
    public int actionsRequired = 2;


    public Animator animator;
    public SpriteRenderer spriteRenderer;
    int current_cooldown = 0;
    public int attack_cooldown;
    int decrease_cooldown_rate = 1;
    public float moveSpeed = 7;
    Vector3 direction;
    Vector3 move;
    GameObject playerObject;
    GameObject parentObject;
    float angle;

    void Start()
    {
        parentObject = this.gameObject.transform.parent.gameObject;
        this.gameObject.transform.parent = null;

        playerObject = GameObject.FindGameObjectWithTag("Player");
        direction = (playerObject.transform.position - transform.position).normalized;
        angle = parentObject.GetComponent<enemyController>().GetAbilityAngle();
        current_cooldown = 0;
    }

    void Update()
    {
        if (angle > 135f && angle < 225f)
        {
            move = transform.up;
        }
        else if (angle > 225f && angle < 315f)
        {
            move = transform.right;
        }
        else if (angle > 45f && angle < 135f)
        {
            move = -transform.right;
        }
        else
        {
            move = -transform.up;
        }
        
        Move(move, moveSpeed);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            other.gameObject.GetComponent<characterController>().takeDamage(damage);
            destroySelf();
        }
        else if (other.tag == "Enemy" && other.gameObject != parentObject)
        {
            other.gameObject.GetComponent<enemyController>().takeDamage(damage);
            destroySelf();
        }
        else if (other.tag == "Obstacle")
        {
            Debug.Log("Hit obstacle");
        }
    }

    private void Move(Vector3 direction, float speed) {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    public int getCurrentCooldown()
    {
        return current_cooldown;
    }

    public void setCurrentCooldown(int value)
    {
        current_cooldown = value;
    }

    public void destroySelf()
    {
        Destroy(gameObject);
    }

    public void isAttacking(bool decision)
    {
        spriteRenderer.enabled = decision;
        animator.enabled = decision;        
    }

    public void startCooldown()
    {
        current_cooldown = attack_cooldown + 1;
    }

    public void decreaseCooldown()
    {
        if (current_cooldown > 0) {
            current_cooldown -= decrease_cooldown_rate;
        }
        else
        {
            current_cooldown = 0;
        }
    }

}