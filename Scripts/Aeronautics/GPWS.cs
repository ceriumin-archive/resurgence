using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPWS : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource _audio;
    [SerializeField] private AudioClip[] landingGPWS;
    [SerializeField] private Rigidbody rb;

    private float altitude;
    void Start()
    {
        rb= GetComponent<Rigidbody>();
    }

    void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, 999999))
        {
            //shoots a raycast down to the ground and ignores the plane's collider to determine the aircraft altitude
            if (hit.collider.gameObject.transform.root == this.gameObject.transform.root || hit.collider.gameObject.layer == 3)
                Physics.IgnoreCollision(hit.collider, GetComponent<Collider>());
            altitude = hit.distance;
            Debug.DrawLine(transform.position, hit.point, Color.green);
        }

        RaycastHit hit2;
        if (Physics.Raycast(transform.position, transform.forward, out hit2, 999999))
        {
            //shots a raycast forward to determine if the plane is about to crash into a mountain
            Debug.DrawLine(transform.position, hit2.point, Color.red);
            if (hit2.collider.gameObject.layer == 8)
                Physics.IgnoreCollision(hit2.collider, GetComponent<Collider>());
        }

        //converts the altitude from meters to feet and rounds it to the nearest whole number
        altitude = altitude * 3.28084f;
        altitude = Mathf.Round(altitude);
        LandingSounds();
    }

    void LandingSounds()
    {
        /* I have used an array to determine the sound clips that are played to make it much more cleaner and easier. When the plane's
           vertical velocity is less than 0, the sound clips will play. The altitude is used to determine which sound clip is played. */
         
        if (altitude == 50f && rb.velocity.y < 0 && !_audio.isPlaying)
            _audio.PlayOneShot(landingGPWS[4]);
        if (altitude == 40f && rb.velocity.y < 0 && !_audio.isPlaying)
            _audio.PlayOneShot(landingGPWS[3]);
        if (altitude == 30f && rb.velocity.y < 0 && !_audio.isPlaying)
            _audio.PlayOneShot(landingGPWS[2]);
        if (altitude == 20f && rb.velocity.y < 0 && !_audio.isPlaying)
            _audio.PlayOneShot(landingGPWS[1]);
        if (altitude == 10f && rb.velocity.y < 0 && !_audio.isPlaying)
            _audio.PlayOneShot(landingGPWS[0]);
    }
}
