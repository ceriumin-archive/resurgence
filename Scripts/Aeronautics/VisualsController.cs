using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using Resurgence;

namespace Resurgence
{ 
    public class VisualsController : MonoBehaviour 
    {

        [Header("Aircraft Graphics")]
        [SerializeField] private Material material;
        [SerializeField] public GameObject explosionFX;

        [Header("Control Surface Models")]
        [SerializeField] Transform rightAileron;
        [SerializeField] Transform leftAileron;
        [SerializeField] List<Transform> elevators;
        [SerializeField] List<Transform> rudders;
        [SerializeField] List<Transform> flaps;
        [SerializeField] Transform airbrake;
        [Space(3)]
        [Range(0, 90)]
        [SerializeField] private float maxAileronAngle;
        [Range(0, 90)]
        [SerializeField] private float maxElevatorAngle;
        [Range(0, 90)]
        [SerializeField] private float maxRudderAngle;
        [Range(0, 90)]
        [SerializeField] private float airbrakeAngle;
        [Range(0, 90)]
        [SerializeField] private float flapsAngle;
        [SerializeField] private float canopyAngle;
        [SerializeField] private float canopySpeed;
        [SerializeField] private float flapsSpeed;
        [SerializeField] private float deflectionSpeed;
        [Space(3)]
        [SerializeField] Transform joystick;
        [SerializeField] Transform canopy;

        [Header("Engine Effects")]
        [SerializeField] Light[] thrusters;
        [SerializeField] ParticleSystem[] afterburners;

        [Header("Landing Gear")]
        [SerializeField] public bool isLandingGearDeployed;
        [SerializeField] WheelCollider[] wheels;
        [SerializeField] GameObject[] suspension;
        [SerializeField] GameObject[] visualWheels;
        [SerializeField] GameObject noseWheel;

        public float brakeSensitivity;
        float brakeTimer;

        [Header("Vortex Effects")]
        [SerializeField] TrailRenderer[] vortexTrails;
        [Tooltip("The minimum AOA required to initialize vortex trails")]
        [SerializeField] float vortexInitializationThreshold;

        [HideInInspector] public List<GameObject> missileGraphics;
        Animator anim;
        Plane plane;
        Dictionary<Transform, Quaternion> neutralPoses;
        Vector3 Angle;
        float airbrakePosition;
        float flapsPosition;
        Physics Physics;

        public bool canopyOpen;

        void Start() {
            plane = GetComponent<Plane>();
            neutralPoses = new Dictionary<Transform, Quaternion>();
            anim = GetComponent<Animator>();

            //calculates the poses at start of the control surfaces to determine the neutral position

            AddNeutralPose(leftAileron);
            AddNeutralPose(rightAileron);

            foreach (var t in elevators) {
                AddNeutralPose(t);
            }

            foreach (var t in rudders) {
                AddNeutralPose(t);
            }

            AddNeutralPose(airbrake);

            foreach (var t in flaps) {
                AddNeutralPose(t);
            }

            Physics = GetComponent<Physics>();
        }

        /* When a missile is fired, the respective missile is dropped as a prefab, and the graphic of the missile is actually set to inactive,
         * either way, it is incredibly seamless, and very optimised*/
        public void ShowMissileGraphic(int index, bool visible) {
            //prevent array out of bounds exception
            if (index >= missileGraphics.Count || missileGraphics[index] == null) return;
            missileGraphics[index].SetActive(visible);
        }

        void AddNeutralPose(Transform transform) {
            neutralPoses.Add(transform, transform.localRotation);
        }

        Quaternion CalculatePose(Transform transform, Quaternion offset) {
            return neutralPoses[transform] * offset;
        }


        void UpdateControlSurfaces(float dt) {
            var input = plane.controlInput;

            //Uses the utilities script to move the control surfaces to the desired angle smoothly

            Angle.x = Utilities.MoveTo(Angle.x, input.x, deflectionSpeed, dt, -1, 1);
            Angle.y = Utilities.MoveTo(Angle.y, input.y, deflectionSpeed, dt, -1, 1);
            Angle.z = Utilities.MoveTo(Angle.z, input.z, deflectionSpeed, dt, -1, 1);

            if (rightAileron == null) return;
            rightAileron.localRotation = CalculatePose(rightAileron, Quaternion.Euler(Angle.z * maxAileronAngle, 0, 0));

            if (leftAileron == null) return;
            leftAileron.localRotation = CalculatePose(leftAileron, Quaternion.Euler(-Angle.z * maxAileronAngle, 0, 0));


            foreach (var t in elevators) {

                if (t == null) return;
                t.localRotation = CalculatePose(t, Quaternion.Euler(Angle.x * maxElevatorAngle, 0, 0));

            }

            foreach (var t in rudders) {
                if (t == null) return;
                t.localRotation = CalculatePose(t, Quaternion.Euler(0, -Angle.y * maxRudderAngle, 0));
            }

            //if the front wheel is grounded, rotate the wheel to the desired angle, and rotate the wheel collider to the desired angle through rudder controls
            if (wheels[0].isGrounded)
            {
                noseWheel.transform.localRotation = Quaternion.Euler(0, Angle.y * 45, 0);
                wheels[0].steerAngle = Angle.y * 45;
            }

            else
            {
                noseWheel.transform.localRotation = Quaternion.Euler(0, 0, 0);
                wheels[0].steerAngle = 0;
            }
        }

        void UpdateFlaps(float dt) {
            /*calculates the flaps rotation. There was much more functionality for this in the past, but it was removed due to the fact that it was not realistic
             * to the specification of the planes, as they have typically one flap setting instead of multiple, which was the plans. */

            var target = plane.FlapsDeployed ? 1 : 0;
            flapsPosition = Utilities.MoveTo(flapsPosition, target, flapsSpeed, dt);

            foreach (var t in flaps) {
                if (t == null) return;
                t.localRotation = CalculatePose(t, Quaternion.Euler(flapsPosition * -flapsAngle, 0, 0));
            }
        }

        void CockpitAnimations()
        {
            //rotates the joystick
            joystick.transform.localRotation = Quaternion.Euler(plane.controlInput * 5);
            //rotate the canopy to canopyangle smoothly, and stop values fluctiating
            if(canopyOpen) canopy.localRotation = Quaternion.Slerp(canopy.localRotation, Quaternion.Euler(-canopyAngle, 0, 0), canopySpeed);
            else canopy.localRotation = Quaternion.Slerp(canopy.localRotation, Quaternion.Euler(0, 0, 0), canopySpeed);


        }

        void Thrusters()
        {
            //sets light intensity equal to the throttle, this is for the engine glow
            foreach (var t in thrusters)
                t.intensity = Mathf.SmoothStep(t.intensity, plane.Throttle * 100, 0.035f);

            //increase the y axis of the afterburners on a lerp when afterburners is true, this is for the afterburner animations and effects
            foreach (var a in afterburners)
            {
                Vector3 temp = a.transform.localScale;
                if (plane.afterburners)
                {
                    //stop the afterburners from flickering
                    if (!a.isPlaying) a.Play();
                    temp.y = Mathf.Lerp(temp.y, 5, 0.015f);
                    a.transform.localScale = temp;
                }
                else
                {
                    temp.y = Mathf.Lerp(temp.y, 0, 0.015f);
                    a.transform.localScale = temp;
                    if (temp.y < 0.1f) a.Stop();
                }
            
            }

        }

        void Gear()
        {
           
            //handles the landing gear, some logic regarding it all so it deploys and retracts properly 
            for (int i = 0; i < wheels.Length; i++)
            {
                if (!wheels[i].isGrounded)
                    isLandingGearDeployed = !plane.landingGear;

                if (isLandingGearDeployed)
                {
                    plane.isGearDeployed = true;
                    anim.SetBool("Gears", false);
                    suspension[i].SetActive(true);

                    //checks if the landing gear is down, and if it is, enable the wheel colliders and disable the suspension

                    if (anim.GetCurrentAnimatorStateInfo(0).IsName("Gear Down"))
                    {
                        wheels[i].enabled = true;
                        wheels[i].GetWorldPose(out Vector3 pos, out Quaternion rot);

                        suspension[i].transform.position = pos;

                        //for some odd reason, the back wheels rotate correctly on the Y axis, and the front on the X axis, so this makes it rotate correctly
                        visualWheels[0].transform.localRotation = Quaternion.Euler(rot.eulerAngles.x, 0, 0);
                        visualWheels[1].transform.localRotation = Quaternion.Euler(0, rot.eulerAngles.x, 0);
                        visualWheels[2].transform.localRotation = Quaternion.Euler(0, rot.eulerAngles.x, 0);

                        if (wheels[i].isGrounded)
                            wheels[i].motorTorque = plane.Throttle;

                        if (plane.wheelBrakes || plane.parkingBrakes)
                        {
                            //apply wheel brakes gradually to prevent strong braking action (useful when wanting to taxi on the ground)
                            brakeTimer += Time.deltaTime;
                            wheels[i].brakeTorque = brakeTimer * brakeSensitivity;
                            wheels[i].motorTorque = 0;
                        }

                        else
                        {
                            brakeTimer = 0;
                            wheels[i].brakeTorque = 0;
                        }

                    }               
                }
                //logic for landing gear being up
                if(!isLandingGearDeployed)
                {
                    plane.isGearDeployed = false;
                    anim.SetBool("Gears", true);
                    suspension[0].GetComponentInChildren<Bulb>().lightOn = false;
                    wheels[i].enabled = false;

                    if (anim.GetCurrentAnimatorStateInfo(0).IsName("Gear Up"))
                        suspension[i].SetActive(false);
                }

            }
  
        }

        void Particles()
        {
            //sets realistic vortex particle trails for the plane based off the Angle of Attack like in real life
            //the vortexinitialisation is the angleofattack required to start the vortex trails, which are longer based off velocity
            float AOA = Physics.AngleOfAttack;

            if (AOA < 0) AOA *= -1;
            if (AOA > vortexInitializationThreshold)
                foreach (var t in vortexTrails)
                    t.time = Mathf.Lerp(t.time, plane.Rigidbody.velocity.magnitude / 10, 0.05f);

            else
                foreach (var t in vortexTrails)
                    t.time = Mathf.Lerp(t.time, 0, 0.05f);
        }

        public void EngineFailure(int index)
        { 
            //no implementation for this as of yet
        }

        private void Update()
        {
            //set the missile graphics equal to the hardpoints count for easier management and customisation later on.
            if (missileGraphics.Count != plane.hardpoints.Count)
            {
                missileGraphics.Clear();
                foreach (var h in plane.hardpoints)
                {
                    if(h == null) return;
                    missileGraphics.Add(h.gameObject);
                }
            }
        }

        void LateUpdate() {
            float dt = Time.deltaTime;

            Particles();
            Thrusters();
            CockpitAnimations();
            UpdateControlSurfaces(dt);
            UpdateFlaps(dt);
            Gear();
        }
    }
}
