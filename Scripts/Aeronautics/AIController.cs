using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Resurgence;
using System;
using TMPro;

namespace Resurgence
{ 
    public class AIController : MonoBehaviour {
        [Header("Steering")]
        [SerializeField] private float steeringSpeed;
        [SerializeField] private float minSpeed;
        [SerializeField] private float maxSpeed;
        [SerializeField] private float recoverSpeedMin;
        [SerializeField] private float recoverSpeedMax;

        [Header("Ground Avoidance")]
        [SerializeField] private LayerMask groundCollisionMask;
        [SerializeField] private float groundCollisionDistance;
        [SerializeField] private float groundAvoidanceAngle;
        [SerializeField] private float groundAvoidanceMinSpeed;
        [SerializeField] private float groundAvoidanceMaxSpeed;

        [Header("Statistics")]
        [SerializeField] private float pitchUpThreshold;
        [SerializeField] private float fineSteeringAngle;
        [SerializeField] private float rollFactor;

        [Header("Missiles")]
        [SerializeField] private float missileLockFiringDelay;
        [SerializeField] private float missileFiringCooldown;
        [SerializeField] private float missileMinRange;
        [SerializeField] private float missileMaxRange;
        [SerializeField] private float missileMaxFireAngle;

        [Header("Cannons")]
        [SerializeField] private float bulletSpeed;
        [SerializeField] private float cannonRange;
        [SerializeField] private float cannonMaxFireAngle;
        [SerializeField] private float cannonBurstLength;
        [SerializeField] private float cannonBurstCooldown;

        [Header("Dodging")]
        [SerializeField] private float minMissileDodgeDistance;
        [SerializeField] private float reactionDelay;
        [SerializeField] private float reactionDelayDistance;

        Resurgence.Physics Physics;
        Plane plane;

        Target selfTarget;
        Plane targetPlane;

        Vector3 lastInput;
        bool isRecoveringSpeed;

        float missileDelayTimer;
        float missileCooldownTimer;

        bool cannonFiring;
        float cannonBurstTimer;
        float cannonCooldownTimer;

        struct ControlInput {
            public float time;
            public Vector3 input;
        }

        Queue<ControlInput> inputQueue;
        List<Vector3> dodgeOffsets;
        const float dodgeUpdateInterval = 0.25f;
        float dodgeTimer;

        void Start() 
        {
            References();
        }

        Vector3 AvoidGround() {
            //makes the plane roll to be level and pitch up if it is about to hit the ground
            var roll = plane.Rigidbody.rotation.eulerAngles.z;
            if (roll > 180f) roll -= 360f;
            return new Vector3(-1, 0, Mathf.Clamp(-roll * rollFactor, -1, 1));
        }

        Vector3 RecoverSpeed() {
            //initiates a recovery maneuver to regain speed
            var roll = plane.Rigidbody.rotation.eulerAngles.z;
            var pitch = plane.Rigidbody.rotation.eulerAngles.x;
            if (roll > 180f) roll -= 360f;
            if (pitch > 180f) pitch -= 360f;
            return new Vector3(Mathf.Clamp(-pitch, -1, 1), 0, Mathf.Clamp(-roll * rollFactor, -1, 1));
        }

        Vector3 GetTargetPosition() {
            if (plane.Target == null)
                return plane.Rigidbody.position;

            //all the calculations to determine the target position are done here based off actual dogfighting techniques
            //the nose is pointed slightly ahead of the target to account for bullet travel time

            var targetPosition = plane.Target.Position;

            if (Vector3.Distance(targetPosition, plane.Rigidbody.position) < cannonRange)
                return Utilities.FirstOrderIntercept(plane.Rigidbody.position, plane.Rigidbody.velocity, bulletSpeed, targetPosition, plane.Target.Velocity);

            return targetPosition;
        }

        Vector3 CalculateSteering(float dt, Vector3 targetPosition) {

            //calculates the general steering of the aircraft based on the target position

            if (plane.Target != null && targetPlane.fuselageHealth == 0)
                return new Vector3();

            //extra PID controller to make the plane have correct and fluid movements when steering
            //unfortunately this is a little buggy and causes the plane to go up and down

            var error = targetPosition - plane.Rigidbody.position;
            error = Quaternion.Inverse(plane.Rigidbody.rotation) * error;  

            var errorDir = error.normalized;
            var pitchError = new Vector3(0, error.y, error.z).normalized;
            var rollError = new Vector3(error.x, error.y, 0).normalized;
            var targetInput = new Vector3();

            var pitch = Vector3.SignedAngle(Vector3.forward, pitchError, Vector3.right);
            if (-pitch < pitchUpThreshold) pitch += 360f;
            targetInput.x = pitch;

            if (Vector3.Angle(Vector3.forward, errorDir) < fineSteeringAngle) {
                targetInput.y = error.x;
            } else {
                var roll = Vector3.SignedAngle(Vector3.up, rollError, Vector3.forward);
                targetInput.z = roll * rollFactor;
            }

            targetInput.x = Mathf.Clamp(targetInput.x, -1, 1);
            targetInput.y = Mathf.Clamp(targetInput.y, -1, 1);
            targetInput.z = Mathf.Clamp(targetInput.z, -1, 1);

            var input = Vector3.MoveTowards(lastInput, targetInput, steeringSpeed * dt);
            lastInput = input;

            return input;
        }

        //Finds the best position to dodge a missile without colliding
        Vector3 GetMissileDodgePosition(float dt, Missile missile) {
            dodgeTimer = Mathf.Max(0, dodgeTimer - dt);
            var missilePos = missile.Rigidbody.position;

            var dist = Mathf.Max(minMissileDodgeDistance, Vector3.Distance(missilePos, plane.Rigidbody.position));

            if (dodgeTimer == 0) {
                var missileForward = missile.Rigidbody.rotation * Vector3.forward;
                dodgeOffsets.Clear();

                //gets the four points around the plane to dodge to based off the missiles positions
                dodgeOffsets.Add(new Vector3(0, dist, 0));
                dodgeOffsets.Add(new Vector3(0, -dist, 0));
                dodgeOffsets.Add(Vector3.Cross(missileForward, Vector3.up) * dist);
                dodgeOffsets.Add(Vector3.Cross(missileForward, Vector3.up) * -dist);

                dodgeTimer = dodgeUpdateInterval;
            }

            //select nearest dodge positions and the most feasible one to dodge to
            float min = float.PositiveInfinity;
            Vector3 minDodge = missilePos + dodgeOffsets[0];

            foreach (var offset in dodgeOffsets) {
                var dodgePosition = missilePos + offset;
                var offsetDist = Vector3.Distance(dodgePosition, plane.Rigidbody.position);

                if (offsetDist < min) {
                    minDodge = dodgePosition;
                    min = offsetDist;
                }
            }

            return minDodge;
        }

        float CalculateThrottle(float minSpeed, float maxSpeed) {
            float input = 0;

            //calcualtes the throttle based on the planes speed and tries to balance it out however this is very basic and would need some work
            if (Physics.LocalVelocity.z < minSpeed) {
                input = 1;
            } else if (Physics.LocalVelocity.z > maxSpeed) {
                input = -1;
            }

            return input;
        }
        void CalculateWeapons(float dt)
        {
            if (plane.Target == null) return;

            CalculateMissiles(dt);
            CalculateCannon(dt);
        }
        void CalculateCannon(float dt)
        {
            //this function calcualtes when the aircraft needs to fire the cannon and when it needs to stop firing
            if (targetPlane.fuselageHealth == 0)
            {
                cannonFiring = false;
                return;
            }

            if (cannonFiring)
            {
                cannonBurstTimer = Mathf.Max(0, cannonBurstTimer - dt);

                if (cannonBurstTimer == 0)
                {
                    cannonFiring = false;
                    cannonCooldownTimer = cannonBurstCooldown;
                    plane.SetCannonInput(false);
                }
            }
            else
            {
                //this is based off the angle of the target and the distance to the target, if the angle is too large or the distance is too far then the plane will not fire
                cannonCooldownTimer = Mathf.Max(0, cannonCooldownTimer - dt);

                //works out the position of the target and the error between the target and the plane
                var targetPosition = Utilities.FirstOrderIntercept(plane.Rigidbody.position, plane.Rigidbody.velocity, bulletSpeed, plane.Target.Position, plane.Target.Velocity);

                var error = targetPosition - plane.Rigidbody.position;
                var range = error.magnitude;
                var targetDir = error.normalized;
                var targetAngle = Vector3.Angle(targetDir, plane.Rigidbody.rotation * Vector3.forward);

                if (range < cannonRange && targetAngle < cannonMaxFireAngle && cannonCooldownTimer == 0)
                {
                    //sets cannon firing to true and sets the burst timer to the burst length for how long the cannon should fire for
                    cannonBurstTimer = cannonBurstLength;
                    plane.SetCannonInput(true);
                }

                else
                    plane.SetCannonInput(false);
            }
        }

        void CalculateMissiles(float dt)
        {
            //same functionality as the cannon but for the missiles
            missileDelayTimer = Mathf.Max(0, missileDelayTimer - dt);
            missileCooldownTimer = Mathf.Max(0, missileCooldownTimer - dt);

            var error = plane.Target.Position - plane.Rigidbody.position;
            var range = error.magnitude;
            var targetDir = error.normalized;
            var targetAngle = Vector3.Angle(targetDir, plane.Rigidbody.rotation * Vector3.forward);

            if (!plane.MissileLocked || !(targetAngle < missileMaxFireAngle || (180f - targetAngle) < missileMaxFireAngle))
            {
                //wont fire the missile if the missile is not locked or the target angle is too large however there is some function to determine if the missile should fire
                //at certain points such as chasing or head on. 
                missileDelayTimer = missileLockFiringDelay;
            }

            if (range < missileMaxRange && range > missileMinRange && missileDelayTimer == 0 && missileCooldownTimer == 0)
            {
                plane.TryFireMissile();
                missileCooldownTimer = missileFiringCooldown;
            }
        }

        void Avoidance(float dt)
        {
            //this function is used for avoiding the ground and missiles and generally anything that could cause the plane to crash
            Vector3 steering;
            float throttle;
            var velocityRot = Quaternion.LookRotation(plane.Rigidbody.velocity.normalized);
            var ray = new Ray(plane.Rigidbody.position, velocityRot * Quaternion.Euler(groundAvoidanceAngle, 0, 0) * Vector3.forward);
            Vector3 targetPosition = plane.Target.Position;

            bool emergency = false;
            //checks if the plane is going to crash into the ground and if it is then it will try to avoid it
            if (UnityEngine.Physics.Raycast(ray, groundCollisionDistance + Physics.LocalAngularVelocity.z, groundCollisionMask.value))
            {
                steering = AvoidGround();
                throttle = CalculateThrottle(groundAvoidanceMinSpeed, groundAvoidanceMaxSpeed);
                emergency = true;
            }
            else
            {
                //all the other avoidance functions are called here

                var incomingMissile = selfTarget.GetIncomingMissile();
                if (incomingMissile != null)
                {
                    targetPosition = GetMissileDodgePosition(dt, incomingMissile);
                    emergency = true;
                }
                else
                {
                    targetPosition = GetTargetPosition();
                }

                if (incomingMissile == null && (Physics.LocalVelocity.z < recoverSpeedMin || isRecoveringSpeed))
                {

                    steering = RecoverSpeed();
                    throttle = 1;
                }
                else
                {
                    steering = CalculateSteering(dt, targetPosition);
                    throttle = CalculateThrottle(minSpeed, maxSpeed);
                }
            }

            inputQueue.Enqueue(new ControlInput
            {
                time = Time.time,
                input = steering
            });

            //all the inputs are added to a queue and then the plane will use the inputs in the queue to control the plane
            while (inputQueue.Count > 0)
            {
                var input = inputQueue.Peek();

                var delay = reactionDelay;

                if (emergency)
                {
                    delay = 0;
                }

                if (Vector3.Distance(targetPosition, plane.Rigidbody.position) < reactionDelayDistance)
                {
                    delay = 0;
                }

                if (input.time + delay <= Time.time)
                {
                    plane.SetControlInput(input.input);
                    inputQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }

            plane.SetControlInput(steering);
            plane.SetThrottleInput(throttle);
        }

        void FixedUpdate() {
            if (plane.Dead) return;
            var dt = Time.fixedDeltaTime;
            //for some odd reason the landing gear is deployed when the plane is spawned so this is to fix that, there is no functionality for the landing gear
            //for AI so it doesnt make a difference
            plane.animation.isLandingGearDeployed = false;
            plane.isGearDeployed = false;

            Avoidance(dt);
            CalculateWeapons(dt);
        }
        void References()
        {
            plane = GetComponent<Plane>();
            selfTarget = plane.GetComponent<Target>();
            targetPlane = plane.Target.GetComponent<Plane>();

            dodgeOffsets = new List<Vector3>();
            inputQueue = new Queue<ControlInput>();

            Physics = GetComponent<Resurgence.Physics>();
        }
    }
}
