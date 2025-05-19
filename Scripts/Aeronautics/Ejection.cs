using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Resurgence;

namespace Resurgence
{ 
    public class Ejection : MonoBehaviour
    {
        [Header("Model References")]
        [SerializeField] private GameObject _canopy;
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject seat;
        [SerializeField] private GameObject parachute;

        [Header("Physic References")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Rigidbody rbc;

        [Header("Ejection Settings")]
        [SerializeField] private float ejectionForce;
        [SerializeField] private float time = 0.1f;

        [Header("Other References")]
        [SerializeField] private Plane plane;
        [SerializeField] private Canvas canvas;
        [SerializeField] private PlayerController controller;
        [SerializeField] private ParticleSystem fx;

        private bool isEjecting;
        private bool ejected;
        void Start()
        {
            parachute.SetActive(false);
            parachute.transform.localScale = Vector3.zero;
            fx.Stop();
        }

        // Update is called once per frame
        void Update()
        {
            Eject();
            //Parachute();
            if (plane == null) return;
        }

        void Eject()
        {
            //temporarily on the old unity input system
            if (Input.GetKey(KeyCode.E) && !ejected)
            {
                /* The script pretty much seperates all the models which are supposed to be ejected and adds a rigidbody to them so they are seperated from the aircraft
                it is quite a messy system, however it worked before although it is not working now, I will try to fix it later on */

                isEjecting = true;
                seat.transform.parent = null;
                _canopy.transform.parent = null;

                rb = seat.AddComponent<Rigidbody>();
                rb = seat.GetComponent<Rigidbody>();
                rbc = _canopy.AddComponent<Rigidbody>();

                rbc.drag = 0.5f;
                rbc.mass = 100f;

                canvas.gameObject.SetActive(false);
                plane.Throttle = 0f;

                rb.mass = 100f;
                rb.drag = 0.5f;
                fx.Play();
                controller.plane = null;
            }

            if (isEjecting && !ejected)
            {
                //adds all the required forces and adds a random force to the canopy to make it more realistic acting
                rb.AddForce((transform.up * ejectionForce) + plane.Rigidbody.velocity, ForceMode.Impulse);
                rbc.AddForce(transform.up * (ejectionForce * 5) + plane.Rigidbody.velocity, ForceMode.Impulse);

                rbc.AddTorque(new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100)), ForceMode.Impulse);

                ejected = true;
                isEjecting = false;
            }

        }

        void Parachute()
        {
            /* All functionality for the parachute deployment to automatically determine when the chute should be deployed, and play an animation for the parachute
            deploying, whilst also adding different values to the rigidbody of the player to make it act like a parachute. Everything else like the seat is then seperated*/

            fx.Stop();
            if (ejected)
            {
                time -= Time.deltaTime;
                if (Input.GetKey(KeyCode.P) || rb.velocity.magnitude < 3f && time < 0)
                    parachute.SetActive(true);

                if (parachute.activeInHierarchy)
                {
                    Vector3 temp = Vector3.Lerp(parachute.transform.localScale, Vector3.one, 0.1f);
                    parachute.transform.localScale = temp;

                    player.transform.parent = null;
                    player.AddComponent<Rigidbody>();
                    player.GetComponent<Rigidbody>().drag = 3f;
                    //rotate the player to 0 slowly
                    player.transform.rotation = Quaternion.Lerp(player.transform.rotation, Quaternion.identity, 0.1f);
                    Destroy(this, 3f);
                }
            }
        }
    }
}
