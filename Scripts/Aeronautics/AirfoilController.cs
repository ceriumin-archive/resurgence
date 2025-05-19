using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirfoilController : MonoBehaviour {
    public enum AirfoilType {
        Horizontal,
        Vertical,
        Fuselage
    }

    [Header("References")]
    [SerializeField] private AirfoilType type;
    [SerializeField] private GameObject airfoilObject;
    [SerializeField] private Plane plane;

    [Header("Statistics")]
    [SerializeField] public float _health;
    [SerializeField] private float collisionDamage;
    [Header("Aerodynamics")]
    [SerializeField] private float sweepAngle;
    [SerializeField] private float liftPower;
    [SerializeField] private float inducedDrag;
    [SerializeField] private Vector3 inputInfluence;
    [SerializeField] private float inputSpeed;
    [SerializeField] private float aoaInputRange;

    [Header("Fuel Systems")]
    [SerializeField] private bool hasFuel;
    [SerializeField] public float fuel;

    [Header("Input"), Tooltip("Trim values are inverted")]
    [SerializeField] private float trim;

    public FixedJoint joint;
    float mass;

    new Transform transform;
    Rigidbody rb;
    float input;

    public AirfoilType Type {
        get {
            return type;
        }
    }

    public float SweepAngle {
        get {
            return sweepAngle;
        }
    }

    public float LiftPower {
        get {
            return liftPower;
        }
    }

    public float InducedDrag {
        get {
            return inducedDrag;
        }
    }

    public Vector3 LocalPosition {
        get {
            return transform.localPosition;
        }
    }

    public float AOABias {
        get {
            return input * aoaInputRange + trim;
        }
    }

    void Start() 
    {
        References();
    }


    public void FuelController()
    {
        if(rb == null) return;
        //calculates the mass from LBS to KG and adds it onto the rigidbodies mass
        var i = mass + (fuel * 0.45359237f);
        if (rb.mass != i)
            rb.mass = i;
    }

    public void HealthController()
    {
        //breaks off the airfoil object if the health is 0, and destroys the airfoil, all the aerodynamics are automatically calculated
        if (_health <= 0)
        {
            if(joint != null)
                joint.breakForce = 0;
            airfoilObject.transform.parent= null;
            airfoilObject.GetComponent<CollisionController>().detached = true;
            Destroy(gameObject);
        }
    }

    public void ApplyDamage(float damage)
    {
        //applies damage to the airfoil from the projectile
        _health -= damage;
    }

    public void ApplyFuel(float fuel)
    {
        //no fuel functionality as of yet but will be used for the fuel system
        this.fuel += fuel;
    }

    public void SetInput(float dt, Vector3 input)
    {
        //sets the influence the input has onto the airfoil
        var influence = Vector3.Scale(input, inputInfluence);
        this.input = Utilities.MoveTo(this.input, influence.x + influence.y + influence.z, inputSpeed, dt, -1, 1);
    }
    private void Update()
    {
        /* The airfoil will be destroyed if it is not detected, this is useful for fuel tanks if one doesn't exist or was dropped off.
        The fuselage has its own health script and it is not needed, since it doesnt require a CollisionController, we can disregard this */

        if (airfoilObject == null || !airfoilObject.activeInHierarchy) Destroy(gameObject);
        if (type != AirfoilType.Fuselage)
            airfoilObject.GetComponent<CollisionController>().airfoil = this;
        if (hasFuel) FuelController();
        HealthController();
    }

    void References()
    {
        transform = GetComponent<Transform>();
        joint = airfoilObject.GetComponent<FixedJoint>();
        rb = airfoilObject.GetComponent<Rigidbody>();

        mass = rb.mass;
    }
}
