using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlaneAI : MonoBehaviour
{
    [SerializeField] private float minimumSpeed;
    [SerializeField] private float maximumSpeed;
    Plane plane;

    void Start()
    {
        plane = GetComponent<Plane>();
    }

    Vector3 GetAIInput()
    {
        //fly towards the target of the plane
        Vector3 target = plane.Target.transform.position;
        Vector3 direction = target - transform.position;
        Vector3 localDirection = transform.InverseTransformDirection(direction);
        Vector3 localRight = transform.InverseTransformDirection(transform.right);
        Vector3 localUp = transform.InverseTransformDirection(transform.up);
        Vector3 localForward = transform.InverseTransformDirection(transform.forward);

        //get the angle between the plane and the target
        float angle = Vector3.Angle(localForward, localDirection);
        //get the angle between the plane and the right vector
        float rightAngle = Vector3.Angle(localRight, localDirection);
        //get the angle between the plane and the up vector
        float upAngle = Vector3.Angle(localUp, localDirection);

        //fly towards the target
        Vector3 input = new Vector3(0, 0, 1);
        //if the target is to the right of the plane, turn right
        if (rightAngle > 90)
        {
            input.x = 1;
        }

        //if the target is to the left of the plane, turn left
        else if (rightAngle < 90)
        {
            input.x = -1;
        }

        else
        {
            input.x = 0;
        }


        return input;
    }

    private void Update()
    {
        //get the input from the AI
        Vector3 input = GetAIInput();
        plane.Throttle = 1;
    }
}
