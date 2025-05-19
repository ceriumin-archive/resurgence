using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


/* Missiles in the game have been experimented with a lot, but for now there is only functionality for Infrared missiles
 * but it is a little confusing as aircraft are unable to detect them, but in this one they can, and they can be spoofed,
 * unfortunately it is incompatible with the projectile collision system as it needs a rigidbody, and it may appear like
 * the missile explodes elsewhere, it still applies damage */

public class Missile : MonoBehaviour
{
    public enum Type
    {
        Infrared,
        Radar
    }
    [Header("Information")]
    [SerializeField] private string _name;
    [TextArea]
    [SerializeField] private string description;
    [SerializeField] Type type;
    [Space(5)]
    [Header("Statistics")]
    [SerializeField] private float damage;
    [SerializeField] private float damageRadius;
    [SerializeField] private float speed;
    [SerializeField] private float lifetime;
    [SerializeField] private float trackingAngle;
    [SerializeField] private float turningGForce;
    [SerializeField] private float spoofChance;
    [SerializeField] private float initiateThrust;
    [Space(5)]
    [Header("References"), SerializeField]
    new MeshRenderer renderer;
    [SerializeField]
    GameObject explosionGraphic;
    [SerializeField]
    GameObject thrustFX;
    [SerializeField]
    GameObject smoke;
    [SerializeField]
    LayerMask collisionMask;

    float _speed;
    Plane owner;
    public Target target;
    bool exploded;
    Vector3 lastPosition;
    public float currentVelocity;
    bool isInitialized;


    public Rigidbody Rigidbody { get; private set; }


    //like all other weapons, missiles are launched by the plane that owns them
    public void Launch(Plane owner, Target target)
    {
        this.owner = owner;
        this.target = target;

        Rigidbody = GetComponent<Rigidbody>();

        lastPosition = Rigidbody.position;
        //sets the missile's velocity to the plane's velocity as by default missiles are launched with no velocity for a short time
        Rigidbody.velocity += owner.Rigidbody.velocity;
        currentVelocity = Rigidbody.velocity.magnitude;

        _speed = currentVelocity;

        thrustFX.SetActive(false);
        smoke.SetActive(false);

        //notifies the target that a missile has been launched at it
        if (target != null) target.NotifyMissileLaunched(this, true);
    }

    void Explode()
    {
        if (exploded) return;

        //when the missile explodes, it applies damage to all colliders within the damage radius to make it more realistic
        //and feasible to use a missile. It also instantiates an explosion graphic and destroys the missile
        var hits = Physics.OverlapSphere(Rigidbody.position, damageRadius, collisionMask.value);

        foreach (var hit in hits)
        {
            //checks all the colliders in the damage radius and applies damage to them
            CollisionController collision = hit.gameObject.GetComponent<CollisionController>();
            if (collision != null && collision != owner)
                collision.ApplyDamage(damage * (1 - (Vector3.Distance(Rigidbody.position, collision.transform.position) / damageRadius)));
        }

        Instantiate(explosionGraphic, transform.position, transform.rotation);
        smoke.transform.parent = null;
        Destroy(gameObject);
        if (target != null) target.NotifyMissileLaunched(this, false);
    }

    void CheckCollision()
    {
        //As mentioned in the summary at the top, this is incompatible with the projectile collision system,
        //and uses the next best thing, which is a raycast to check if the missile has collided with anything.
        var currentPosition = Rigidbody.position;
        var error = currentPosition - lastPosition;
        var ray = new Ray(lastPosition, error.normalized);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, error.magnitude, collisionMask.value))
        {
            CollisionController other = hit.collider.gameObject.GetComponent<CollisionController>();

            if (hit.collider.gameObject.transform.root == owner.gameObject.transform.root)
                return;

            if (other == null || other != owner)
            {
                Rigidbody.position = hit.point;
                Explode();
            }
        }

        lastPosition = currentPosition;
    }

    //this function is used to work out the positon of the target and track it correctly. It uses the First Order Intercept
    void TrackTarget(float dt)
    {
        if (target == null) return;
        var targetPosition = Utilities.FirstOrderIntercept(Rigidbody.position, Vector3.zero, speed, target.Position, target.Velocity);

        //works out the distance and angle, also aiming the missile infront of the target.
        //if the angle is greater than the tracking angle, the missile will not track the target
        var dist = targetPosition - Rigidbody.position;
        var targetDir = dist.normalized;
        var currentDir = Rigidbody.rotation * Vector3.forward;

        if (Vector3.Angle(currentDir, targetDir) > trackingAngle)
            return;

        //works out the maximum turn rate of the missile to make it more realistic
        float maxTurnRate = (turningGForce * 9.81f) / speed; 
        var dir = Vector3.RotateTowards(currentDir, targetDir, maxTurnRate * dt, 0);

        //rotates the missile to face the direction of travel
        Rigidbody.rotation = Quaternion.LookRotation(dir);
    }

    private void Update()
    {

        //wait for the initiate thrust timer to expire before starting to accelerate the missile
        //this is common in actual missiles to avoid the missile hitting the plane that launched it
        if (initiateThrust > 0)
        {
            initiateThrust -= Time.deltaTime;
            return;
        }

        if (initiateThrust <= 0)
        {
            isInitialized = true;
            _speed = Mathf.MoveTowards(_speed, speed, 5f);
            Rigidbody.velocity = transform.forward * _speed;
            thrustFX.SetActive(true);
            smoke.SetActive(true);
        }

        Destroy(gameObject, lifetime);
        
    }

    void FixedUpdate()
    {

        if (exploded) return;

        CheckCollision();
        if(isInitialized)
            TrackTarget(Time.fixedDeltaTime);
        TrackIR();

    }   

    void TrackIR()
    {
        /*checks if the missile has been spoofed by flares
         * it sorts all the flares in the scene by distance and angle
         * and randomly cycles through targets. However the missile
         * still has a chance to track the target */

        if (Random.Range(0, 100) < spoofChance)
        {
            var targets = FindObjectsOfType<Target>();
            var t = targets[Random.Range(0, targets.Length)];

            var targetDir = t.transform.position - transform.position;
            var forward = transform.forward;
            var angle = Vector3.Angle(targetDir, forward);
            var distance = Vector3.Distance(t.transform.position, transform.position);

            if (angle < 180 && distance < 3000)
            {
                if (t == owner.gameObject.GetComponent<Target>())
                    return;
                else
                    target = t;
            }
        }
    }
}
