using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationController : MonoBehaviour
{
    Animator animator;
    public SpriteRenderer spriteRenderer;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        
    }

    public void destroySelf() {
        Destroy(gameObject);
    }

    public void isAttacking(bool decision) {
        spriteRenderer.enabled = decision;
        animator.enabled = decision;
    }
}