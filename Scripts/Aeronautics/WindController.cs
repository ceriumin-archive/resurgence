using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindController : MonoBehaviour
{

    public enum Type
    {
        Global,
        Local,
        Disabled
    }

    [Header("Global Settings")]
    [SerializeField] private Type type;
    [Range(0, 360)]
    [SerializeField] private int windDirection;
    [SerializeField] private int WindSpeed;
    [SerializeField] private int windSpeedChangeRate;

    [Header("Local Settings")]
    [SerializeField] private float windRadius;

    [Header("Statistics")]
    [SerializeField] private int windSpeed;

    [Header("Air Density")]
    [SerializeField] private float airDensity;
    [SerializeField] private float airDensityChangeRate;


    Vector3 dir;


    void ForceHandler()
    {
        if (type == Type.Global)
        {
            //adds wind force to all rigidbodies in the scene
            foreach (var rb in FindObjectsOfType<Rigidbody>())
            {
                rb.AddForce(dir * windSpeed, ForceMode.Force);
                Debug.DrawRay(rb.transform.position, dir * windSpeed, Color.red);
            }

        }

        if(type == Type.Local)
        {
            //adds wind force to all rigidbodies in a specific area 
            foreach (var rb in FindObjectsOfType<Rigidbody>())
            {
                if (Vector3.Distance(rb.transform.position, transform.position) < windRadius)
                {
                    rb.AddForce(dir * windSpeed, ForceMode.Force);
                    Debug.DrawRay(rb.transform.position, dir * windSpeed, Color.red);
                }
            }
        }

        if(type == Type.Disabled)
            return;
    }

    void CalculateWind()
    {
        //set the angle to match 360 degrees
        float angle = windDirection * Mathf.Deg2Rad;

        //make windspeed fluctuate based off of windSpeedChangeRate
        windSpeed = WindSpeed + Mathf.RoundToInt(Mathf.Sin(Time.time * windSpeedChangeRate));

        //set the dir to match the angle of wind direction
        dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
        dir.Normalize();
    }

    void CalculateAirDensity()
    {



    }

    // Update is called once per frame
    void Update()
    {
        ForceHandler();
        CalculateWind();
    }
}
