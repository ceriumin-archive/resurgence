using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/* A script for projectils which instead of using Rigidbodies to calculate the position of the bullets, it uses time
 * which works incredibly well for optimised bullets. But most importantly, works well for incredibly high speed projectiles
 * which usually miss their target due to the physics engine not being able to keep up with the collision and speed of the bullets,
 * and typically skips the collision over the next frame. This script is a solution to that problem.*/

public class Bullet : MonoBehaviour
{
    [Header("Information")]
    [SerializeField] private string _name;
    [TextArea]
    [SerializeField] private string description;
    [Space(5)]
    [Header("Statistics")]
    [SerializeField] private float damage;
    [SerializeField] private float lifetime;
    [SerializeField] private float _spread;
    [SerializeField] private float penetration;
    [SerializeField] LayerMask collisionMask;
    [Header("Type")]
    [SerializeField] private bool isExplosive;
    [SerializeField] private bool isIncendiary;
    [SerializeField] private float impactForce;
    [Space(5)]
    [Header("References")]
    [SerializeField] GameObject[] collideFX;
    [Space(5)]
    [Header("Decals")]
    [SerializeField] GameObject[] concrete;
    [SerializeField] GameObject[] metal;
    [SerializeField] GameObject[] glass;

    Plane owner;
    private float gravity;
    private Vector3 startPosition;
    private Vector3 startForward;

    private bool isInitialized;
    private float startTime = -1;

    private float speed;

    //A function for easier management of firing projectiles
    public void Fire(Plane owner, float speed, Transform startpoint)
    {
        this.owner = owner;

        //add spread to the bullet based off random unitinside circle
        Vector3 spread = Random.insideUnitCircle * (_spread / 1000);
        startForward = startpoint.forward + spread;
        startPosition = startpoint.position;

        //sets the velocity of the bullet to the speed of the plane to account in Newtons first law
        this.speed = speed;
        this.speed += owner.Rigidbody.velocity.magnitude;
        gravity = 9.81f;
        isInitialized = true;
    }

    private Vector3 FindPosition(float time)
    {
        //works out the position of the bullet based off the time, speed and gravity. It is automatically calculated from the point the bullet was fired
        Vector3 point = startPosition + startForward * speed * time;
        Vector3 gravityVector = Vector3.down * gravity * time * time;
        return point + gravityVector;
    }

    private bool CastRayBetweenPoints(Vector3 start, Vector3 end, out RaycastHit hit)
    {
        //casts a ray between the start of the bullet, and where it ends, which then anything in the way of the bullet will be hit for more accurate collisions
        return Physics.Raycast(start, end - start, out hit, (end - start).magnitude);
    }

    private void FixedUpdate()
    {
        if (!isInitialized)
            return;
        if (startTime < 0)
            startTime = Time.time;

        RaycastHit hit;
        //The points are worked out based off the time, and then the ray is cast between the points to find the collision
        float currentTime = Time.time - startTime;
        float prevTime = currentTime - Time.fixedDeltaTime;
        float nextTime = currentTime + Time.fixedDeltaTime;

        if (prevTime > 0)
        {
            Vector3 prevPoint = FindPosition(prevTime);
        }

        //functions to find the position of the bullet at the current time, and the previous and next time it will be at
        Vector3 currentPosition = FindPosition(currentTime);
        Vector3 prevPosition = FindPosition(prevTime);
        Vector3 nextPosition = FindPosition(nextTime);

        transform.LookAt(nextPosition);

        if(CastRayBetweenPoints(prevPosition, nextPosition, out hit))
        {
            //another function for drawing a raycast between the previous and next position of the bullet, which then if it hits anything, it will apply damage to it
            CollisionController otherA = hit.collider.gameObject.GetComponent<CollisionController>();
            Plane otherB = hit.collider.gameObject.GetComponent<Plane>();

            if (hit.collider.gameObject.transform.root == owner.gameObject.transform.root)
                return;

            if (otherA != null)
                otherA.ApplyDamage(damage);
            if (otherB != null)
                otherB.ApplyDamage(damage);

            Impact(hit);
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInitialized || startTime < 0)
            return;

        float currentTime = Time.time - startTime;
        Vector3 currentPosition = FindPosition(currentTime);
        transform.position = currentPosition;

        //rotate bullet to face direction of travel
        transform.LookAt(FindPosition(currentTime + 0.01f));

        Destroy(gameObject, lifetime);
    }

    //This calculates impacts of the bullet, and then spawns the correct particle effects and decals

    void Impact(RaycastHit hit)
    {
        //0 references concrete surfaces as an example of the array
        if (hit.collider.gameObject.tag == "Concrete")
        {
            GameObject fx = Instantiate(collideFX[0], hit.point, transform.rotation) as GameObject;
            fx.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
            fx.transform.SetParent(hit.transform);

            GameObject decalObject = Instantiate(concrete[Random.Range(0, concrete.Length)], hit.point, Quaternion.identity) as GameObject;
            //decals are spawned slightly above the surface to prevent it from clipping into the surface
            decalObject.transform.position += hit.normal * 0.001f;
            decalObject.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
            decalObject.transform.SetParent(hit.transform);
        }

        if (hit.collider.gameObject.tag == "Metal" || hit.collider.gameObject.layer == 8)
        {
            GameObject fx = Instantiate(collideFX[1], hit.point, transform.rotation) as GameObject;
            fx.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
            fx.transform.SetParent(hit.transform);

            GameObject decalObject = Instantiate(metal[Random.Range(0, metal.Length)], hit.point, Quaternion.identity) as GameObject;
            decalObject.transform.position += hit.normal * 0.001f;
            decalObject.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
            decalObject.transform.SetParent(hit.transform);
        }
        if (hit.collider.gameObject.tag == "Glass")
        {
            GameObject decalObject = Instantiate(glass[Random.Range(0, glass.Length)], hit.point, Quaternion.identity) as GameObject;
            decalObject.transform.position += hit.normal * 0.001f;
            decalObject.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
            decalObject.transform.SetParent(hit.transform);
          
            //ignores the collision if it is glass
            Physics.IgnoreCollision(GetComponent<Collider>(), hit.collider);
        }

        if (hit.collider.gameObject.tag == "Player")
        {
            //kills the player if it is hit by a bullet, I dont think anyone would survive a 30mm to the face
            Debug.Log("Player Killed");
            Destroy(GameObject.FindGameObjectWithTag("Player"));
        }

    }
}
