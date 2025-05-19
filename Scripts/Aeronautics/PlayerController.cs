using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;
using Resurgence;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Resurgence
{
    public class PlayerController : MonoBehaviour
    {
        public Plane plane;
        [SerializeField]
        HUDController planeHUD;

        [Header("Character Controller")]
        [SerializeField] private bool isMovementEnabled;
        [SerializeField] public GameObject playerObject;
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _jumpForce = 5f;
        [SerializeField] private float _health = 100f;

        [Header("G-Force Controller")]
        [SerializeField] private bool tolerateGForces;
        [Range(0, 20)]
        [SerializeField] private float minG;
        [Range(0, 20)]
        [SerializeField] private float maxG;
        [SerializeField] private float gForceLerpSpeed;
        [SerializeField] private float withstandTimer;
        [SerializeField] private float gLocTime;

        [Header("Camera Controller")]
        [SerializeField] private float cameraSensitivity = 200f;
        [SerializeField] private float shakeMagnitude;
        [HideInInspector] public Transform cameraTransform;

        [Header("Interaction Controller")]
        [SerializeField] public float interactionRange = 2f;
        [SerializeField] public float interactionRadius = 0.5f;
        [SerializeField] public LayerMask interactionMask;
        [Space(3)]
        [SerializeField] private float entranceAndExitSpeed;

        [Header("Death Screen")]
        [SerializeField] private GameObject deathScreen;

        float sustainedGs;
        float time;
        float time2;
        bool isUnconscious;
        float gForce;

        new Camera camera;
        private Volume volume;
        private CameraShake Shake;

        bool isDead;

        float xAxisRotation = 0f;
        float yAxisRotation = 0f;

        CharacterController _characterController;

        private bool isGrounded = true;
        private float gravity = 9.81f;
        private float verticalVelocity = 0f;

        Vector2 lookInput;
        Vector3 controlInput;
        Vector3 playerControlInput;


        bool planeActive;
        Physics Physics;
        float speed;

        void Start()
        {
            time = Random.Range(0, gLocTime);
            time2 = withstandTimer;

            volume = playerObject.GetComponentInChildren<Volume>();
            camera = playerObject.GetComponentInChildren<Camera>();
            cameraTransform = camera.GetComponent<Transform>();
            _characterController = playerObject.GetComponent<CharacterController>();
            Shake = playerObject.GetComponentInChildren<CameraShake>();

            if (plane != null)
            {
                Physics = plane.GetComponent<Physics>();
                planeHUD.SetPlane(plane);
                planeHUD.SetCamera(camera);
            }

        }

        private void FixedUpdate()
        {
            MovementHandler();
        }


        /*Allows the player to set the plane they are flying. This functionality was removed for the demo, but evidence of it working
        can be seen in the respective media folder. The player can walk up to the plane, press E and the player will enter the plane.
        This is incredibly useful for multiplayer situations where the player can enter the plane and the plane will be locked to them only*/

        public void SetPlane(Plane plane)
        {
            //only if the plane's canopy is open
            if (!plane.animation.canopyOpen) return;
            this.plane = plane;
            Physics = plane.GetComponent<Physics>();

            StartCoroutine(PlaneEnter());
            playerObject.transform.parent = plane.sitPoint; 

            if (planeHUD != null)
            {
                planeHUD.SetPlane(plane);
                planeHUD.SetCamera(camera);
            }
        }

        //all input setters for the player
        public void SetThrottleInput(InputAction.CallbackContext context)
        {
            if (plane == null) return;
            plane.SetThrottleInput(context.ReadValue<float>());
        }

        public void OnRollPitchInput(InputAction.CallbackContext context)
        {
            var input = context.ReadValue<Vector2>();
            controlInput = new Vector3(input.y, controlInput.y, -input.x);
            playerControlInput = input;
        }

        public void OnYawInput(InputAction.CallbackContext context)
        {
            if (plane == null) return;

            var input = context.ReadValue<float>();
            controlInput = new Vector3(controlInput.x, input, controlInput.z);
        }

        public void OnCameraInput(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }

        public void OnFlapsInput(InputAction.CallbackContext context)
        {
            if (plane == null) return;

            if (context.phase == InputActionPhase.Performed)
            {
                plane.ToggleFlaps();
            }
        }

        public void OnFireMissile(InputAction.CallbackContext context)
        {
            if (plane == null) return;

            if (context.phase == InputActionPhase.Performed)
            {
                plane.TryFireMissile();
            }
        }

        public void OnFireCannon(InputAction.CallbackContext context)
        {
            
            if(plane == null) return;

            if (context.phase == InputActionPhase.Started)
            {
                plane.SetCannonInput(true);
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                plane.SetCannonInput(false);
            }
        }

        public void OnBrakesApplied(InputAction.CallbackContext context)
        {
            if (plane == null) return;

            if (context.phase == InputActionPhase.Started)
            {
                plane.SetBrakesInput(true);
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                plane.SetBrakesInput(false);
            }
        }

        public void OnParkingBrakesApplied(InputAction.CallbackContext context)
        {
            if (plane == null) return;

            if (context.phase == InputActionPhase.Performed)
            {
                plane.SetParkingBrake();
            }
        }

        public void OnLandingGear(InputAction.CallbackContext context)
        {
            if (plane == null) return;

            if (context.phase == InputActionPhase.Performed)
            {
                plane.SetLandingGear();
            }
        }

        public void OnFireFlare(InputAction.CallbackContext context)
        {
            if (plane == null) return;

            if (context.phase == InputActionPhase.Started)
            {
                plane.SetFlareInput(true);
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                plane.SetFlareInput(false);
            }
        }

        void DeathController()
        {
            //sets the simple death screen to active if the player is dead
            if (playerObject == null)
                deathScreen.SetActive(true);

        }

        public void CameraController()
        {
            //all the camera functionality is here. Everything is clamped to prevent the camera from going too far unrealistically, and mimick realistic head movements

            float X = lookInput.x * cameraSensitivity * Time.deltaTime;
            float Y = lookInput.y * cameraSensitivity * Time.deltaTime;

            yAxisRotation -= Y;
            yAxisRotation = Mathf.Clamp(yAxisRotation, -90f, 50f);
            xAxisRotation -= X;
            if(plane != null) xAxisRotation = Mathf.Clamp(xAxisRotation, -120f, 120f);

            cameraTransform.localRotation = Quaternion.Euler(yAxisRotation, -xAxisRotation, 0f);

            if (cameraTransform != null)
                return;
        }

        void MovementHandler()
        {
            //All the movement functionality is here so the player would be able to exit/enter the plane and walk around the map

            if (!isMovementEnabled || plane != null) return;
            float x = playerControlInput.x;
            float y = playerControlInput.y;

            Vector3 move = (transform.right * x + transform.forward * y) * _speed;
            _characterController.Move(move * Time.deltaTime);

            transform.rotation = Quaternion.Euler(0f, cameraTransform.rotation.eulerAngles.y, 0f);

            //add gravity to the player
            verticalVelocity -= gravity * Time.deltaTime;
            Vector3 gravityVector = new Vector3(0, verticalVelocity, 0);
            _characterController.Move(gravityVector * Time.deltaTime);

        }

        public void GravityController()
        {
            //All the gravity functionality is here. The player would be able to withstand a certain amount of Gs before passing out

            if (plane == null) return;
            gForce = Physics.gforce;

            //clamps the gForce to a certain min/max value and sets the volume weight to the gForce which is now between 0 and 1
            sustainedGs = Mathf.Clamp(gForce, minG, maxG);
            sustainedGs = Mathf.InverseLerp(minG, maxG, gForce);

            if (gForce > minG)
            {
                Shake.Shake(true, sustainedGs * shakeMagnitude);
                time2 -= Time.deltaTime;
                if (time2 <= 0)
                {
                    volume.weight = Mathf.Lerp(volume.weight, 1, gForceLerpSpeed * Time.deltaTime);
                    if (volume.weight > 0.9f)
                        isUnconscious = true;
                }

            }

            else
            {
                Shake.Shake(false, 0);
                volume.weight = Mathf.Lerp(volume.weight, 0, 0.025f);
                if (volume.weight <= 0.01f)
                {
                    volume.weight = 0f;
                    time2 = withstandTimer;
                }
            }

            if (isUnconscious)
            {
                //cuts all controls and sets the volume weight to 1 to simulate the player passing out
                volume.weight = 1f;
                time -= Time.deltaTime;
                //level off the plane
                plane.SetControlInput(Vector3.zero);
                planeHUD.gameObject.SetActive(false);
                if (time <= 0)
                    isUnconscious = false;
            }


        }

        //disable collisions with the root object
        void DisableCollisions()
        {
            if (plane == null) return;
            //disable collisions with the plane object to prevent the player from colliding with it
            Collider[] colliders = plane.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                UnityEngine.Physics.IgnoreCollision(collider, _characterController);
            }

        }

        //Functionality for entering the plane and moving the camera to the cockpit

        IEnumerator PlaneEnter()
        {
            var sitPos = plane.sitPoint.position;
            var playerPos = playerObject.transform.position;
            //smoothly move the player to the plane
            while (Vector3.Distance(playerPos, sitPos) > 0.01f)
            {
                playerPos = Vector3.Lerp(playerPos, sitPos, Time.deltaTime * entranceAndExitSpeed);
                playerObject.transform.position = playerPos;
                yield return null;
            }
        }

        void Update()
        {
            DeathController();
            DisableCollisions();
            CameraController();


            if (plane == null)
            {
                volume.weight = 0f;
                return;
            }
            else planeActive = true;
            if (!isUnconscious)
            {
                planeHUD.gameObject.SetActive(true);
                plane.SetControlInput(controlInput);
                time = Random.Range(0, gLocTime);
            }
            if (tolerateGForces)
                GravityController();
        }
    }
}
