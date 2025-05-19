using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

//Please read the Aeronautic Theory documentation for much more context behind the Physics

namespace Resurgence
{
    [RequireComponent(typeof(Rigidbody))]
    public class Physics : MonoBehaviour
    {
        [Header("Lift Physics")]
        [SerializeField] private float liftPower;
        [SerializeField] private float rudderLiftPower;
        [SerializeField] private float flapsLiftPower;
        [SerializeField] AnimationCurve angleOfAttackCurve;
        [SerializeField] AnimationCurve rudderAngleOfAttackCurve;
        [SerializeField] AnimationCurve flapsAngleOfAttackCurve;

        [Header("Induced Drag")]
        [SerializeField] private float inducedDragPower;
        [SerializeField] AnimationCurve inducedDragCurve;

        [Header("Airfoil References")]
        [SerializeField] List<AirfoilController> airfoils;
        [SerializeField] List<AirfoilController> flapAirfoils;

        [Header("Steering Physics")]
        [SerializeField] private Vector3 steeringPower;
        [SerializeField] private Vector3 steeringAcceleration;
        [SerializeField] AnimationCurve steeringCurve;
        [SerializeField] private float steeringBias;
        [SerializeField] private float steeringDamping;
        [SerializeField] BoxCollider steeringCollider;
        [Space(3)]
        [SerializeField] PIDController pitchPID;
        [SerializeField] PIDController rollPID;
        [SerializeField] PIDController yawPID;

        [Header("Drag Physics")]
        [SerializeField] AnimationCurve dragForward;
        [SerializeField] AnimationCurve dragBackwards;
        [SerializeField] AnimationCurve dragLeft;
        [SerializeField] AnimationCurve dragRight;
        [SerializeField] AnimationCurve dragTop;
        [SerializeField] AnimationCurve dragBottom;
        [Space(3)]
        [SerializeField] private Vector3 angularDrag;
        [Header("Environmental Physics")]
        [SerializeField] private LayerMask altitudeMask;
        [SerializeField] private AnimationCurve airDensityCurve;

        [HideInInspector] public Vector3 LocalVelocity;

        public Vector3 EffectiveInput { get; private set; }
        public Vector3 Velocity { get; private set; }
        public Vector3 lastVelocity { get; private set; }
        public Vector3 LocalGForce { get; private set; }
        public Vector3 LocalAngularVelocity { get; private set; }

        public Rigidbody Rigidbody { get; private set; }
        public Plane plane { get; private set; }

        //values for monitoring purposes
        public float dragCoefficient { get; private set; }
        public float liftCoefficient { get; private set; }
        public float gforce { get; private set; }
        public float AngleOfAttack { get; private set; }
        public float AngleOfAttackYaw { get; private set; }
        public float airDensity { get; private set; }
        public float altitude { get; private set; }



        private void Start()
        {
            References();
        }

        void CalculateAngleOfAttack()
        {
            //The angle of Attack is calculated based off the velocity of the plane, which is put through on a Tan function from a Tan curve
            //The angle of attack is then used to calculate the lift and drag of the plane

            if (LocalVelocity.sqrMagnitude < 0.1f)
            {
                AngleOfAttack = 0;
                AngleOfAttackYaw = 0;
                return;
            }

            AngleOfAttack = Mathf.Atan2(-LocalVelocity.y, LocalVelocity.z);
            AngleOfAttackYaw = Mathf.Atan2(LocalVelocity.x, LocalVelocity.z);
        }

        void CalculateGForce(float dt)
        {
            //GForce is calculated by taking the difference between the current velocity and the last velocity, and then dividing it by the time passed
            //This is then converted into local space, and then the Y value is taken to get the GForce
            var invRotation = Quaternion.Inverse(Rigidbody.rotation);
            var acceleration = (Velocity - lastVelocity) / dt;
            LocalGForce = invRotation * acceleration;
            
            //gforce is reduced when going faster to prevent silly values
            LocalGForce *= Mathf.Clamp01(1 - (LocalVelocity.z / 1000));
            lastVelocity = Velocity;

            //Gforce is converted to positive if it is negative although negative GForces do exist, they are not used in this game
            gforce = LocalGForce.y / 9.81f;
            if (gforce < 0)
                gforce = -gforce;
        }

        void CalculateState()
        {
            //Velocity values are calculated here for use later on in the physics
            var invRotation = Quaternion.Inverse(Rigidbody.rotation);
            Velocity = Rigidbody.velocity;
            LocalVelocity = invRotation * Velocity;
            LocalAngularVelocity = invRotation * Rigidbody.angularVelocity;

            //the localvelocity is made positive so the lift is still into account when going backwards
            if (LocalVelocity.z < 0)
                LocalVelocity.z = -LocalVelocity.z;

            CalculateAngleOfAttack();
        }

        void UpdateDrag()
        {
            //Velocity squared is part of the lift and drag coefficient 
            var lv = LocalVelocity;
            var lv2 = lv.sqrMagnitude;

            //Drag for different parts of the plane are calculated here and then added together
            float airbrakeDrag = plane.AirbrakeDeployed ? plane.airbrakeDrag : 0;
            float flapsDrag = plane.FlapsDeployed ? plane.flapsDrag : 0;
            float wheelDrag = plane.isGearDeployed ? plane.wheelDrag : 0;
            float externalDrag = plane.fuelTanks.Length * plane.fuelTankDrag;

            //Drag coefficient is calculated here which is used to determine the drag force on different dimensions of the plane
            var coefficient = Utilities.Scale6(
                lv.normalized,
                dragRight.Evaluate(Mathf.Abs(lv.x)), dragLeft.Evaluate(Mathf.Abs(lv.x)),
                dragTop.Evaluate(Mathf.Abs(lv.y)), dragBottom.Evaluate(Mathf.Abs(lv.y)),
                dragForward.Evaluate(Mathf.Abs(lv.z)) + airbrakeDrag + flapsDrag + wheelDrag + externalDrag,
                dragBackwards.Evaluate(Mathf.Abs(lv.z))
            );

            var drag = coefficient.magnitude * lv2 * -lv.normalized;
            dragCoefficient = drag.magnitude;

            //all the drag is then used against the Rigidbody 
            Rigidbody.AddRelativeForce(drag);
        }

        Vector3 CalculateLift(float angleOfAttack, float sweepAngle, Vector3 rightAxis, float liftPower, float inducedDragPower, AnimationCurve aoaCurve, AnimationCurve inducedDragCurve)
        {
            
            //Air density is calculated here using the altitude of the plane so that the lift is reduced at higher altitudes
            var airDensity = 1.225f * Mathf.Exp(-0.000118f * altitude);
            var liftVelocity = LocalVelocity * Mathf.Cos(sweepAngle);
            var v2 = liftVelocity.sqrMagnitude;

            var liftCoefficient = aoaCurve.Evaluate(angleOfAttack * Mathf.Rad2Deg);
            var liftForce = v2 * liftCoefficient * liftPower;

            //make the liftforce smaller depending on the airdensity curve
            liftForce *= airDensityCurve.Evaluate(airDensity);
            var liftDirection = Vector3.Cross(liftVelocity.normalized, rightAxis);
            var lift = liftDirection * liftForce;

            //every other force is calculated here such as induced drag which has a direct correlation with the lift coefficient
            var dragForce = liftCoefficient * liftCoefficient;
            var dragDirection = -liftVelocity.normalized;
            var inducedDrag = dragDirection * v2 * dragForce * inducedDragPower * inducedDragCurve.Evaluate(Mathf.Max(0, LocalVelocity.z));

            this.liftCoefficient = liftCoefficient;
            this.airDensity = airDensity;

            return lift + inducedDrag;
        }

        void UpdateAirfoil(AirfoilController airfoil)
        {
            float aoa;
            float aoaSweep;
            Vector3 direction;

            //All the airfoils are calculated here, the direction of the airfoil is determined by the type of airfoil it is, all the values are then put into the CalculateLift function
            if (airfoil.Type == AirfoilController.AirfoilType.Horizontal || airfoil.Type == AirfoilController.AirfoilType.Fuselage)
            {
                aoa = AngleOfAttack + airfoil.AOABias * Mathf.Deg2Rad;
                aoaSweep = AngleOfAttackYaw + airfoil.SweepAngle * Mathf.Deg2Rad;
                direction = Vector3.right;

            }

            else
            {
                aoa = AngleOfAttackYaw + airfoil.AOABias * Mathf.Deg2Rad;
                aoaSweep = AngleOfAttack + airfoil.SweepAngle * Mathf.Deg2Rad;
                direction = Vector3.up;
            }

            var force = Rigidbody.rotation * CalculateLift(aoa, aoaSweep, direction, airfoil.LiftPower, airfoil.InducedDrag, angleOfAttackCurve, inducedDragCurve);
            var position = Rigidbody.position + Rigidbody.rotation * airfoil.LocalPosition;
            Rigidbody.AddForceAtPosition(force, position);

        }

        void UpdateLift(float dt)
        {
            /*Other external forces are calculated here such as the lift from the wings and the lift from the fuselage which might have a influence on the lift. This
            also includes everything such as PID controllers and the steering curve*/

            var speed = Mathf.Max(0, LocalVelocity.z);
            var av = LocalAngularVelocity;

            var steerPower = steeringCurve.Evaluate(speed);

            var gForceScaling = CalculateGLimiter(plane.controlInput, steeringPower * Mathf.Deg2Rad * steerPower);
            var targetAV = Vector3.Scale(steeringPower, plane.controlInput) * gForceScaling;

            float x = pitchPID.Update(dt, av.x * Mathf.Rad2Deg, targetAV.x * 2.5f);
            float y = yawPID.Update(dt, av.y * Mathf.Rad2Deg, targetAV.y);
            float z = rollPID.Update(dt, av.z * Mathf.Rad2Deg, targetAV.z);

            EffectiveInput = new Vector3(x, y, z);

            foreach (var airfoil in airfoils)
            {
                airfoil.SetInput(dt, EffectiveInput);
                if (airfoil == null) return;
                UpdateAirfoil(airfoil);
            }

            foreach (var airfoil in flapAirfoils)
            {
                airfoil.SetInput(dt, new Vector3(plane.FlapsDeployed ? 1 : 0, 0, 0));
                if (airfoil == null) return;
                UpdateAirfoil(airfoil);
            }

        }

        void UpdateAngularDrag()
        {
            //Angular drag is calculated here which is used to determine the angular drag force on different dimensions of the plane
            var av = LocalAngularVelocity;
            var drag = av.sqrMagnitude * -av.normalized;
            Rigidbody.AddRelativeTorque(Vector3.Scale(drag, angularDrag), ForceMode.Acceleration);
        }

        /*G Forces are all used here, the G forces are calculated using the angular velocity and the velocity of the plane, there is a G Limiter
        set onto the plane which limits the amount of G's the plane can pull, this is used to prevent the plane from going under impossible G loads*/

        Vector3 CalculateGForce(Vector3 angularVelocity, Vector3 velocity)
        {
            return Vector3.Cross(angularVelocity, velocity);
        }

        Vector3 CalculateGForceLimit(Vector3 input)
        {
            var gforce = plane.gLimit;
            var gforcePitch = plane.gLimitPitch;
            return Utilities.Scale6(input,
                gforce, gforcePitch,
                gforce, gforce,
                gforce, gforce
            ) * 9.81f;
        }

        float CalculateGLimiter(Vector3 controlInput, Vector3 maxAngularVelocity)
        {
            if (controlInput.magnitude < 0.01f)
                return 1;

            var maxInput = controlInput.normalized;

            var limit = CalculateGForceLimit(maxInput);
            var maxGForce = CalculateGForce(Vector3.Scale(maxInput, maxAngularVelocity), LocalVelocity);

            if (maxGForce.magnitude > limit.magnitude && !plane.overrideGLimiter)
                return limit.magnitude / maxGForce.magnitude;

            return 1;
        }

        private void UpdateResistance()
        {
            //A box collider is set to calculate the air resistance of the plane, which overlaps the whole physics collider, this is scaled to the velocity of the plane
            //for stability during very high speeds. A strange way of doing it but it works.

            Vector3 steering = new Vector3(steeringBias, steeringBias, steeringBias);
            steeringCollider.size = steering * Rigidbody.velocity.magnitude / steeringDamping;
            if(steeringCollider.size.x < steering.x)
                steeringCollider.size = steering;
        }

        private void CalculateAltitude()
        {
            //shoot a raycast down to the ground to calculate the altitude of the plane
            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, altitudeMask))
                altitude = hit.distance;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            CalculateState();
            CalculateGForce(dt);

            UpdateLift(dt);
            UpdateDrag();
            UpdateAngularDrag();
            UpdateResistance();
            CalculateAltitude();
        }


        private void References()
        {
            steeringCollider.size = new Vector3(steeringBias, steeringBias, steeringBias);
            Rigidbody = GetComponent<Rigidbody>();
            plane = GetComponent<Plane>();

        }

    }
}
