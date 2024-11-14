using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animationController : MonoBehaviour
{
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public characterController characterController;

    void Start()
    {
        // animator = GetComponent<Animator>();
    }

    void Update()
    {
        
    }

    public void destroySelf() {
        Destroy(gameObject);
        gameObject.transform.parent.GetComponent<characterController>().stopAiming(false);
    }

    public void isAttacking(bool decision) {
        spriteRenderer.enabled = decision;
        animator.enabled = decision;
    }
}