using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMouse : MonoBehaviour
{


    void Update()
    {
        transform.position = GetMouseWorldPosition();
        //Debug.Log(mouseWorldPosition); // Check if it's working
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // Get mouse position
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Plane at Y=0

        float rayDistance;
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            return ray.GetPoint(rayDistance); // Return the world position of the mouse
        }

        return Vector3.zero; // Default fallback
    }
}
