using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Resurgence
{

    //General debug information for the purpose of debugging the physics of the plane
    public class DebugInformation : MonoBehaviour
    {
        [Header("Lift")]
        [SerializeField] private float liftCoefficient;
        [SerializeField] private float angleOfAttack;
        [SerializeField] private string gForce;

        [Header("Drag")]
        [SerializeField] private float dragCoefficient;

        [Header("Weight")]
        [SerializeField] private string totalWeight;
        [SerializeField] private string zeroFuelWeight;
        [SerializeField] private string fuelWeight;

        [Header("Thrust")]
        [SerializeField] private float thrust;

        [Header("Plane Speed")]
        [SerializeField] private bool isSupersonic;
        [SerializeField] private string speedInMach;
        [SerializeField] private string speedInKnots;
        [SerializeField] private string speedInMPH;
        [SerializeField] private string speedInKPH;
        [SerializeField] private string speedInMPS;
        [Space(5)]
        [SerializeField] private int startingSpeed;

        [Header("Player Input")]
        [SerializeField] private Vector3 controlInput;

        [Header("Environmental Physics")]
        [SerializeField] private float airDensity;
        [SerializeField] private float altitude;

        Physics Physics;
        Plane plane;
        float speed;

        // Start is called before the first frame update
        void Start()
        {
            Physics = GetComponent<Physics>();
            plane = GetComponent<Plane>();
            Physics.Rigidbody.velocity = Physics.Rigidbody.rotation * new Vector3(0, 0, startingSpeed);
        }

        // Update is called once per frame
        void Update()
        {
            Display();
        }

        void Display()
        {
            //functions and values to work out all the values through calculations to change them into the correct units
            altitude = Physics.altitude;
            airDensity = Physics.airDensity;
            controlInput = plane.controlInput;

            liftCoefficient = Physics.liftCoefficient;
            dragCoefficient = Physics.dragCoefficient / 10000;
            if(dragCoefficient < 10) dragCoefficient = 0;
            angleOfAttack = Physics.AngleOfAttack;
            thrust = plane.currentPower;

            Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
            float totalMass = 0;
            foreach (Rigidbody rigidbody in rigidbodies)
                totalMass += rigidbody.mass;

            totalWeight = string.Format("{0:0} lbs", totalMass * 2.20462262f);
            zeroFuelWeight = string.Format("{0:0} lbs", totalMass - plane._totalFuel * 2.20462262f);
            fuelWeight = string.Format("{0:0} lbs", plane._totalFuel * 2.20462262f);
          

            speed = Physics.Rigidbody.velocity.magnitude;
            gForce = string.Format("{0:0}G", Physics.gforce);

            speedInMach = string.Format("{0:0.00} Mach", speed / 343);
            speedInKnots = string.Format("{0:0.00} Knots", speed * 1.94384449f);
            speedInMPH = string.Format("{0:0.00} MPH", speed * 2.23693629f);
            speedInKPH = string.Format("{0:0.00} KPH", speed * 3.6f);
            speedInMPS = string.Format("{0:0.00} MPS", speed);

            if (speed > 343)
                isSupersonic = true;
            else
                isSupersonic = false;
        }
    }
}
