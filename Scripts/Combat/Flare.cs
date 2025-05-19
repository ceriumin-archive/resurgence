using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flare : MonoBehaviour
{
    [Header("Statistics")]
    [SerializeField] float lifetime;
    [SerializeField] float speed;

    Plane owner;
    new Rigidbody rigidbody;

    float startTime;

    //not much functionality here as it is just a simple gameobject
    public void Fire(Plane owner) {
        this.owner = owner;
        rigidbody = GetComponent<Rigidbody>();
        startTime = Time.time;

        rigidbody.AddRelativeForce(new Vector3(0, 0, speed), ForceMode.VelocityChange);
        rigidbody.AddForce(owner.Rigidbody.velocity, ForceMode.VelocityChange);
    }

    void FixedUpdate() 
    {
        if (Time.time > startTime + lifetime) {
            Destroy(gameObject);
            return;
        }
    }
}
