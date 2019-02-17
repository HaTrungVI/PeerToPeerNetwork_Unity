using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMoving : MonoBehaviour 
{
    public float speed = 10f;
    public float speedScroll = 10f;
    public float maxDistanceToScroll = 3f;

    public Transform LocalCamera;
    private Vector3 LocalCameraDir = Vector3.zero;
    private float distance = 0f;
	void Start () 
    {
        LocalCameraDir = LocalCamera.forward.normalized;
	}
	void Update () 
    {
        if (!MouseOutScreen())
        {
            Vector3 dir = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
            dir = dir.normalized;
            Vector3 targetPosition = transform.position + dir;
            transform.position = Vector3.Slerp(transform.position, targetPosition, Time.deltaTime * speed);
        }

        float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        distance += mouseScroll * 2;
        if (distance >= maxDistanceToScroll)
        {
            distance = maxDistanceToScroll;
        }
        if (distance <= -maxDistanceToScroll)
        {
            distance = -maxDistanceToScroll;
        }

        Vector3 LocalCamTarget = transform.position + LocalCameraDir * distance;
        LocalCamera.position = Vector3.Slerp(LocalCamera.position, LocalCamTarget, Time.deltaTime * speedScroll);
	}

    private bool MouseOutScreen()
    {
        return false;

        Vector3 targetPosition = Vector3.zero;

        bool re = false;

        if (Input.mousePosition.x <= 0) // trai
        {
            targetPosition.x -= Time.deltaTime * speed * 100;
            re = true;
        }
        if (Input.mousePosition.x >= Screen.width - 1f) // phai
        {
            targetPosition.x += Time.deltaTime * speed * 100;
            re = true;
        }

        if (Input.mousePosition.y <= 0) // duoi
        {
            targetPosition.z -= Time.deltaTime * speed * 100;
            re = true;
        }

        if (Input.mousePosition.y >= Screen.height - 1f) // tren
        {
            targetPosition.z += Time.deltaTime * speed * 100;
            re = true;
        }
        targetPosition = targetPosition.normalized;
        targetPosition = transform.position + targetPosition;

        transform.position = Vector3.Slerp(transform.position, targetPosition, Time.deltaTime * speed);
        return re;

    }
}
