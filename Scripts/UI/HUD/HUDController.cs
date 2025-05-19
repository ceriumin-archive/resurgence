using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Resurgence;

namespace Resurgence
{ 
    public class HUDController : MonoBehaviour {
        [Header("Statistics")]
        [SerializeField] private float updateRate;
        [SerializeField] private bool centerHUD = true;

        [Header("Colors")]
        [SerializeField] private Color normalColor;

        [Header("References")]
        [SerializeField] private Compass compass;
        [SerializeField] private PitchLadder pitchLadder;
        [SerializeField] private Transform hudCenter;
        [SerializeField] private Transform velocityMarker;
        [SerializeField] private Text airspeed;
        [SerializeField] private Text gforceIndicator;
        [SerializeField] private Text altitude;
        [SerializeField] private Transform targetBox;
        [SerializeField] private Text targetName;
        [SerializeField] private Text targetRange;
        [SerializeField] private Transform missileLock;
        [SerializeField] private Transform reticle;
        [SerializeField] private RectTransform reticleLine;

        [Header("Weapons")]
        [SerializeField] private float cannonRange;
        [SerializeField] private float bulletSpeed;


        [SerializeField]
        List<Graphic> missileWarningGraphics;

        Plane plane;
        public Physics Physics;
        Target selfTarget;
        Transform planeTransform;
        new Camera camera;
        Transform cameraTransform;

        GameObject hudCenterGO;
        GameObject velocityMarkerGO;
        GameObject targetBoxGO;
        Image targetBoxImage;
        GameObject missileLockGO;
        Image missileLockImage;
        GameObject reticleGO;

        float lastUpdateTime;

        const float metersToKnots = 1.94384f;
        const float metersToFeet = 3.28084f;

        void Start() {
            References();
        }

        //This also uses the same method as projectiles in which the plane is set to the HUD
        public void SetPlane(Plane plane) {
            this.plane = plane;

            if (plane == null) {
                planeTransform = null;
                selfTarget = null;
            }
            else {
                planeTransform = plane.GetComponent<Transform>();
                selfTarget = plane.GetComponent<Target>();
            }

            if (compass != null)
                compass.SetPlane(plane);

            if (pitchLadder != null)
                pitchLadder.SetPlane(plane);

            Physics = plane.GetComponent<Physics>();
        }

        public void SetCamera(Camera camera) {
            //The positioning of the pitch ladder is based around the camera to work out the angle of the pitch ladder 
            this.camera = camera;

            if (camera == null) {
                cameraTransform = null;
            } else {
                cameraTransform = camera.GetComponent<Transform>();
            }

            if (compass != null) {
                compass.SetCamera(camera);
            }

            if (pitchLadder != null) {
                pitchLadder.SetCamera(camera);
            }
        }

        void UpdateVelocityMarker() {
            //The velocity marker is the small triangle that points in the direction of the velocity of the plane
            //this is worked out by the velocity of the plane and the direction it is facing
            var velocity = planeTransform.forward;

            if (Physics.LocalVelocity.sqrMagnitude > 1) {
                velocity = Physics.Rigidbody.velocity;
            }

            var hudPos = TransformToHUDSpace(cameraTransform.position + velocity);

            if (hudPos.z > 0) {
                velocityMarkerGO.SetActive(true);
                velocityMarker.localPosition = new Vector3(hudPos.x, hudPos.y, 0);
            } else {
                velocityMarkerGO.SetActive(false);
            }
        }

        //These functions are simply just simple HUD functions that are used to update the HUD text, they are converted 
        //to their correct and respective units

        void UpdateAirspeed() {
            var speed = Physics.LocalVelocity.z * metersToKnots;
            airspeed.text = string.Format("{0:0}", speed);
        }


        void UpdateGForce() {
            var gforce = Physics.LocalGForce.y / 9.81f;
            gforceIndicator.text = string.Format("{0:0.0} G", gforce);
        }

        void UpdateAltitude() {
            var altitude = plane.Rigidbody.position.y * metersToFeet;
            this.altitude.text = string.Format("{0:0}", altitude);
        }

        Vector3 TransformToHUDSpace(Vector3 worldSpace) {
            var screenSpace = camera.WorldToScreenPoint(worldSpace);
            return screenSpace - new Vector3(camera.pixelWidth / 2, camera.pixelHeight / 2);
        }

        //This function is important to make the HUD appear at the center and give it that centered effect.
        //if the boolean is true
        void UpdateHUDCenter() {
            if (centerHUD)
            {
                var rotation = cameraTransform.localEulerAngles;
                var hudPos = TransformToHUDSpace(cameraTransform.position + planeTransform.forward);

                if (hudPos.z > 0)
                {
                    hudCenterGO.SetActive(true);
                    hudCenter.localPosition = new Vector3(hudPos.x, hudPos.y, 0);
                    hudCenter.localEulerAngles = new Vector3(0, 0, -rotation.z);
                }
                else
                {
                    hudCenterGO.SetActive(false);
                }
            }
        }

        void UpdateWeapons() {

            if (plane.Target == null) {
                targetBoxGO.SetActive(false);
                missileLockGO.SetActive(false);
                return;
            }

            //functions for updating the weapons and the target box, giving it that lock on effect
            var targetDistance = Vector3.Distance(plane.Rigidbody.position, plane.Target.Position);
            var targetPos = TransformToHUDSpace(plane.Target.Position);
            var missileLockPos = plane.MissileLocked ? targetPos : TransformToHUDSpace(plane.Rigidbody.position + plane.MissileLockDirection * targetDistance);

            if (targetPos.z > 0) {
                //as simple as setting the target box to true and setting the position of the target box
                targetBoxGO.SetActive(true);
                targetBox.localPosition = new Vector3(targetPos.x, targetPos.y, 0);
            } else {
                targetBoxGO.SetActive(false);
            }

            if (plane.MissileTracking && missileLockPos.z > 0) {
                missileLockGO.SetActive(true);
                missileLock.localPosition = new Vector3(missileLockPos.x, missileLockPos.y, 0);
            } else {
                missileLockGO.SetActive(false);
            }

            //visuals for displaying the target name and target range 
            targetName.text = plane.Target.Name;
            targetRange.text = string.Format("{0:0 m}", targetDistance);

            //this is the function for the cannon range, it is used to display the cannon range and the cannon lead
            //it is the little reticle that appears on the HUD to show where the cannon will hit
            var leadPos = Utilities.FirstOrderIntercept(plane.Rigidbody.position, plane.Rigidbody.velocity, bulletSpeed, plane.Target.Position, plane.Target.Velocity);
            var reticlePos = TransformToHUDSpace(leadPos);

            if (reticlePos.z > 0 && targetDistance <= cannonRange) {
                reticleGO.SetActive(true);
                reticle.localPosition = new Vector3(reticlePos.x, reticlePos.y, 0);

                //These functions are negated if the target is behind the plane, this is to make the reticle appear
                var reticlePos2 = new Vector2(reticlePos.x, reticlePos.y);
                if (Mathf.Sign(targetPos.z) != Mathf.Sign(reticlePos.z)) reticlePos2 = -reticlePos2;   
                var targetPos2 = new Vector2(targetPos.x, targetPos.y);
                var reticleError = reticlePos2 - targetPos2;

                var lineAngle = Vector2.SignedAngle(Vector3.up, reticleError);
                reticleLine.localEulerAngles = new Vector3(0, 0, lineAngle + 180f);
                reticleLine.sizeDelta = new Vector2(reticleLine.sizeDelta.x, reticleError.magnitude);
            } else {
                reticleGO.SetActive(false);
            }
        }

        void References()
        {
            hudCenterGO = hudCenter.gameObject;
            velocityMarkerGO = velocityMarker.gameObject;
            targetBoxGO = targetBox.gameObject;
            targetBoxImage = targetBox.GetComponent<Image>();
            missileLockGO = missileLock.gameObject;
            missileLockImage = missileLock.GetComponent<Image>();
            reticleGO = reticle.gameObject;

            targetBoxImage.color = normalColor;
            targetName.color = normalColor;
            targetRange.color = normalColor;
            missileLockImage.color = normalColor;
        }

        void LateUpdate() {
            if (plane == null) return;
            if (camera == null) return;

            float degreesToPixels = camera.pixelHeight / camera.fieldOfView;

            if (!plane.Dead) {
                UpdateVelocityMarker();
                UpdateHUDCenter();
            } else {
                hudCenterGO.SetActive(false);
                velocityMarkerGO.SetActive(false);
            }

            UpdateAirspeed();
            UpdateAltitude();
            UpdateWeapons();

            //update rate is the refresh rate of the HUD to make it appear smooth and not laggy
            if (Time.time > lastUpdateTime + (1f / updateRate)) {
                UpdateGForce();
                lastUpdateTime = Time.time;
            }
        }
    }
}
