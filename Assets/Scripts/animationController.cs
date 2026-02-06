using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationController : MonoBehaviour
{
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public characterController characterController;
    public int damage;
    int current_cooldown = 0;
    public int attack_cooldown;
    int decrease_cooldown_rate = 1;

    void Start()
    {
        // animator = GetComponent<Animator>();\
        current_cooldown = 0;
    }

    void Update()
    {

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
        gameObject.transform.parent.GetComponent<characterController>().stopAiming(false);
    }

    public void isAttacking(bool decision)
    {
        spriteRenderer.enabled = decision;
        animator.enabled = decision;        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Enemy")
        {
            other.gameObject.GetComponent<enemyController>().takeDamage(damage);
        }
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