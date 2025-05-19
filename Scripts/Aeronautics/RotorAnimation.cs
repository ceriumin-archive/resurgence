using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotorAnimation : MonoBehaviour
{
    [SerializeField] private Transform rotor;

    void Update()
    {
        //very basic to get the rotor spinning for the helicopter functionality to demo it 
        rotor.Rotate(0, 5000 * Time.deltaTime, 0);
    }
}
