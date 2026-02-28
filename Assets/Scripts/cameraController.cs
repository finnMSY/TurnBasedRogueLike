using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{
    public GameObject cameraPoint;

    public float smoothSpeed = 2f;
    public float arrivalThreshold = 0.01f;

    private Transform cameraTarget;
    public bool isMoving = false;

    void Start()
    {
        this.transform.position = cameraPoint.transform.position;
    }

    void Update()
    {
        if (isMoving)
        {
            bool arrived = moveCamera(cameraTarget);
            if (arrived)
            {
                isMoving = false;
            }
        }
    }

    public void StartMoveCamera(Transform trans)
    {
        cameraTarget = trans;
        isMoving = true;
    }

    private bool moveCamera(Transform trans)
    {
        Vector3 targetPosition = new Vector3(trans.position.x, trans.position.y, this.transform.position.z);
        this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        return Vector3.Distance(this.transform.position, targetPosition) < arrivalThreshold;
    }
}