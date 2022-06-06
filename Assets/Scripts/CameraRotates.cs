using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotates : MonoBehaviour
{
    public Transform target;
    public float angularVel = 1f;

    void LateUpdate()
    {
        // Vector3 direction = transform.position - target.position;
        // Quaternion rotation = Quaternion.LookRotation(direction);
        // direction = Quaternion.AngleAxis(1, transform.up) * direction;
        // Quaternion newRotation = Quaternion.LookRotation(direction);
        //
        //
        // transform.position = Quaternion.RotateTowards(rotation, newRotation,
        //     Time.deltaTime * angularVel) * direction + target.position;
        
        transform.RotateAround(target.GetComponent<MeshFilter>().mesh.bounds.center, Vector3.up, Time.deltaTime * angularVel);
    }
}