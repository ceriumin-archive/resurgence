using Resurgence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionController : MonoBehaviour
{
    [HideInInspector] public AirfoilController airfoil;
    [HideInInspector] public bool detached;
    [HideInInspector] private MeshCollider collider;
    [HideInInspector] private GameObject explosionFX;

    private void OnCollisionEnter(Collision collision)
    {
        //if the part collides with something, apply relative velocity damage to the part, as it is more accurate of determining the velocity of the collision. 
        if (airfoil == null) return;
        if (collision.gameObject.transform.root == this.gameObject.transform.root)
            UnityEngine.Physics.IgnoreCollision(collision.collider, this.gameObject.GetComponent<Collider>());
        else
        {
            airfoil.ApplyDamage(collision.relativeVelocity.magnitude * 3f);
            Debug.Log(this.gameObject.name + " collided at a speed of " + collision.relativeVelocity.magnitude + " m/s");
        }
    }

    //ignore collisions with the gameobject's that this script is attached to
    private void OnCollisionStay(Collision collision)
    {
        if (airfoil == null) return;
        if (collision.gameObject.transform.root == this.gameObject.transform.root)
            UnityEngine.Physics.IgnoreCollision(collision.collider, this.gameObject.GetComponent<Collider>());
    }

    private void OnDrawGizmos()
    {
        if (collider == null) collider = GetComponent<MeshCollider>();
        if (airfoil == null) return;

        if (!Application.isPlaying) return;


        //draws gizmos for the part's health as a wireframe for debugging purposes.

        Gizmos.color = Color.Lerp(Color.red, Color.green, airfoil._health / 1000f);
        Gizmos.DrawWireMesh(collider.sharedMesh, transform.position, transform.rotation, transform.localScale);
    }

    public void ApplyDamage(float damage)
    {
        //function which projectiles execute when they hit the part.
        if (airfoil == null) return;
        airfoil.ApplyDamage(damage);
        Debug.Log(gameObject.name + " took " + damage + " damage");
    }

    private void Start()
    {
        explosionFX = GetComponentInParent<VisualsController>().explosionFX;
    }

    private void Update()
    {
        if (GetComponent<Joint>() == null)
            detached = true;

        //if it is detached, and it is moving fast enough and collides, explode.
        if(detached && GetComponent<Rigidbody>().velocity.magnitude > 20f)
        {
            Instantiate(explosionFX, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

}
