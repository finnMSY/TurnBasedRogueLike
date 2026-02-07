using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class destroyOnAnimComplete : MonoBehaviour
{
    public float delay;

    void Start()
    {
        float animTime = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        Destroy(gameObject, animTime + delay);   
    }
}
