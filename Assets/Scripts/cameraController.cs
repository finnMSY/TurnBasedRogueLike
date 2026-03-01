using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraController : MonoBehaviour
{
    public GameObject cameraPoint;

    public float smoothSpeed = 2f;
    public float arrivalThreshold = 0.01f;

    private Vector3 cameraTarget;
    public bool isMoving = false;

    private Vector3 lastPosition;

    void Start()
    {
        isMoving = false; // force reset regardless of Inspector value
        cameraTarget = transform.position; // also safe-initialize target to current pos
        lastPosition = transform.position;
    }

    void Update()
    {
        if (transform.position != lastPosition)
        {
            Debug.Log("Camera moved to: " + transform.position + "\n" + System.Environment.StackTrace);
            lastPosition = transform.position;
        }

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
        Debug.Log("Moving camera");
        cameraTarget = trans.position;
        isMoving = true;
    }

    private bool moveCamera(Vector3 targetPos)
    {
        Vector3 targetPosition = new Vector3(targetPos.x, targetPos.y, this.transform.position.z);
        this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        return Vector3.Distance(this.transform.position, targetPosition) < arrivalThreshold;
    }
}