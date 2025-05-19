
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

//Script generally used for all the plane statistic, control and weapon information as well as some other things described below

public class AircraftController : MonoBehaviour {

    [Header("Information")]
    [SerializeField] Type type;
    [SerializeField] private string _name;

    [Header("Power Plant")]
    [SerializeField] private string name;
    [SerializeField] public float currentPower;
    [SerializeField] private float maxPower;
    [SerializeField] private AnimationCurve thrustCurve;
    [SerializeField] private float thrustAcceleration;
    [SerializeField] private bool hasAfterburners;
    [SerializeField] private float afterburnerPower;
    [SerializeField] private Transform[] engines;

    [Header("Drag Statistics")]
    [SerializeField] public float flapsDrag;
    [SerializeField] public float airbrakeDrag;
    [SerializeField] public float wheelDrag;

    [Header("Armaments")]
    [SerializeField] public List<Transform> hardpoints;
    [SerializeField] private float missileReloadTime;
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private Target target;
    [SerializeField] private float lockRange;
    [SerializeField] private float lockSpeed;
    [SerializeField] private float lockAngle;
    [Header("Cannon")]
    [Tooltip("Firing rate in Rounds Per Minute")]
    [SerializeField] private float cannonFireRate;
    [SerializeField] private float ammunition;
    [SerializeField] private float cannonSpread;
    [SerializeField] private float cannonSpeed;
    [SerializeField] private Transform[] muzzle;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Countermeasures")]
    [SerializeField]
    [Tooltip("Firing rate in Rounds Per Minute")]
    float flareFireRate;
    [SerializeField]
    float flareSpread;
    [SerializeField]
    Transform flarePoint;
    [SerializeField]
    GameObject measuresPrefab;

    [Header("Fuel")]
    [SerializeField] public float _totalFuel;
    [SerializeField] private float fuelConsumption;
    [SerializeField] private float centreFuel;
    [SerializeField] private AirfoilController leftWing;
    [SerializeField] private AirfoilController rightWing;
    [SerializeField] public AirfoilController[] fuelTanks;
    [SerializeField] public float fuelTankDrag;

    [Header("Damage Controller")]
    [SerializeField] public float fuselageHealth;
    [SerializeField] private float collisionDamage;

    [Space(3)]
    [Range(0, 100)]
    [SerializeField] private float engineFailureChance;
    [Range(0, 100)]
    [SerializeField] private float hydraulicFailureChance;
    [Range(0, 100)]
    [SerializeField] private float electricalFailureChance;
    [Range(0, 100)]
    [SerializeField] private float avionicsFailureChance;
    [Range(0, 100)]
    [SerializeField] private float fuelLeakChance;
    [Range(0, 100)]
    [SerializeField] private float fuelFireChance;

    [Header("Debug")]
    [SerializeField] private bool flapsDeployed;
    [SerializeField] public bool overrideGLimiter;

    [Header("Misc")]
    [SerializeField] public Transform sitPoint;
    [SerializeField] public float gLimit;
    [SerializeField] public float gLimitPitch;
    [SerializeField] public bool afterburners;
    [SerializeField] private float throttleSpeed;

    int arrayPos;

    float throttleInput;

    int missileIndex;
    List<float> missileReloadTimers;
    Vector3 missileLockDirection;

    bool cannonFiring;
    float cannonFiringTimer;

    bool flareFiring;
    float flareFiringTimer;

    float mass;
    bool canReload;

    [HideInInspector]
    public float Throttle;
    float thrust;
    [HideInInspector]
    public bool isGearDeployed;

    public bool Dead { get; private set; }

    public Rigidbody Rigidbody { get; private set; }
    public bool AirbrakeDeployed { get; private set; }
    public bool wheelBrakes { get; private set; }
    public bool parkingBrakes { get; private set; }
    public bool landingGear { get; private set; }
    public float gforce { get; private set; }

    public VisualsController animation { get; private set; }

    public bool isActive { get; private set; }

    public bool FlapsDeployed {
        get {
            return flapsDeployed;
        }
        private set {
            flapsDeployed = value;

        }
    }

    public bool MissileLocked { get; private set; }
    public bool MissileTracking { get; private set; }
    public bool isEngineFailure { get; private set; }
    public bool isHydraulicFailure { get; private set; }
    public bool isElectricalFailure { get; private set; }
    public Vector3 controlInput { get; private set; }

    public Target Target {
        get {
            return target;
        }
    }
    public Vector3 MissileLockDirection {
        get {
            return Rigidbody.rotation * missileLockDirection;
        }
    }

    void Start() {
        animation = GetComponent<VisualsController>();
        Rigidbody = GetComponent<Rigidbody>();

        missileReloadTimers = new List<float>(hardpoints.Count);

        foreach (var h in hardpoints) {
            missileReloadTimers.Add(0);
        }

        missileLockDirection = Vector3.forward;
        Cursor.visible = false;
        mass = Rigidbody.mass;
    }

    public void ApplyDamage(float damage) {
        //applies damage to the fuselage only
        fuselageHealth -= damage;
        DamageController();
    }

    void UpdateThrottle(float dt) {
        float target = 0;
        if (throttleInput > 0) target = 1;
        
        //move the throttle to the target value
        Throttle = Utilities.MoveTo(Throttle, target, throttleSpeed * Mathf.Abs(throttleInput), dt);
        AirbrakeDeployed = Throttle == 0 && throttleInput == -1;
    }

    void UpdateThrust()
    {
        if(type == Type.FixedWing)
        {
            /*The thrust is calculated by the throttle input and the thrust curve to give more of a realistic feel rather than
            having it be an instant change in thrust, allows it to spool up etc*/

            thrust = Mathf.Clamp01(thrust + (Throttle - thrust) * thrustCurve.Evaluate(Time.deltaTime * thrustAcceleration));
            if (thrust < 0.01f && Throttle == 0) thrust = 0;
            if (thrust > 0.99f) thrust = 1;
            currentPower = thrust * maxPower;

            for (int i = 0; i < engines.Length; i++)
            {
                /*Since this is for fixed wing, the thrust is applied to the engines forward direction. Afterburners are applied if the bool is true which multiply the thrust by the afterburner power
                Engines are also indexed so that if there are multiple engines, the thrust is applied to each one, which is useful for engine failures. They act realistically, so if one was to fail,
                the other one would still work but cause some aerodynamic issues such as veering off to the side*/

                if (afterburners & hasAfterburners)
                    Rigidbody.AddForceAtPosition(engines[i].transform.forward * currentPower * afterburnerPower, engines[i].transform.position);
                else
                    Rigidbody.AddForceAtPosition(engines[i].transform.forward * currentPower, engines[i].transform.position);
            }
        }

        //Very experimental functionality for helicopters

        if (type == Type.RotorCraft)
        {
            //sets an idle throttle to keep the engine running
            if (Throttle < 0.35f)
                Throttle = 0.35f;

            thrust = Throttle * maxPower;
            currentPower = thrust;

            for (int i = 0; i < engines.Length; i++)
            {
                //thrust is applied to the engines up direction
                Rigidbody.AddForceAtPosition(engines[0].transform.up * currentPower, engines[0].transform.position);
                //balance the helicopter forces using the tail rotor and addrelativetorque
                Rigidbody.AddRelativeTorque(Vector3.up * currentPower * (controlInput.y * 2));

                //rotates the rotors
                engines[1].transform.Rotate(50000 * Time.deltaTime, 0, 0);
                //moves the thrust rotation of the rotors to move the helicopter
                engines[0].transform.localRotation = Quaternion.Euler(controlInput.x * 10, 0 , controlInput.z * 10);
            }
        }
    }

    public void TryFireMissile() {
        if (Dead) return;

        for (int i = 0; i < hardpoints.Count; i++) {
            var index = (missileIndex + i) % hardpoints.Count;
            if (missileReloadTimers[index] == 0) {
                /*fires the missile and sets the reload timer based off the hardpoint index
                which also sets the missile graphics*/
                
                FireMissile(index);

                missileIndex = (index + 1) % hardpoints.Count;
                missileReloadTimers[index] = missileReloadTime;

                animation.ShowMissileGraphic(index, false);
                break;
            }
        }
    }

    void FireMissile(int index) {
        //functionality for firing the correct missile index, this is also used to determine which plane owns the missile and who it is locked onto
        var hardpoint = hardpoints[index];
        var missileGO = Instantiate(missilePrefab, hardpoint.position, hardpoint.rotation);
        var missile = missileGO.GetComponent<Missile>();
        missile.Launch(this, MissileLocked ? Target : null);

    }

    void FuelController(float dt)
    {
        for(int i = 0; i < fuelTanks.Length; i++)
        {
            //adds all the fuel together
            _totalFuel = leftWing.fuel + centreFuel + rightWing.fuel + fuelTanks[i].fuel;

            //calculates the mass from LBS to KG and adds it onto the rigidbodies mass
            //the measurement for airplane fuel is typically in pounds as opposed to litres (measurement for rigidbodies is in kg)
            var j = mass + (centreFuel * 0.45359237f);
            if (Rigidbody.mass != j)
                Rigidbody.mass = j;
        }
    }

    void UpdateWeapons(float dt) {
        UpdateWeaponCooldown(dt);
        UpdateMissileLock(dt);
        UpdateCannon(dt);
        UpdateFlare(dt);

        if (hardpoints == null) return;
    }

    void UpdateWeaponCooldown(float dt) 
    {
        cannonFiringTimer = Mathf.Max(0, cannonFiringTimer - dt);
        flareFiringTimer = Mathf.Max(0, flareFiringTimer - dt);

        if(animation != null)
            if (animation.missileGraphics == null) return;

        for (int i = 0; i < hardpoints.Count; i++)
        {
            //automatically removes the hardpoint if it is null to add some form of weapon customisation
            if (hardpoints[i] == null)
            {
                hardpoints.RemoveAt(i);
                missileReloadTimers.RemoveAt(i);
            }
        }

        //changes the missile graphic if the missile has reloaded
        for (int i = 0; i < missileReloadTimers.Count; i++) {
            missileReloadTimers[i] = Mathf.Max(0, missileReloadTimers[i] - dt);

            if (missileReloadTimers[i] == 0) {
                animation.ShowMissileGraphic(i, true);
            }
        }



    }

    void UpdateMissileLock(float dt) {
        //missile lock functionality, which determines where the missile is locked onto and if it is locked onto anything

        Vector3 targetDir = Vector3.forward;
        MissileTracking = false;

        if (Target != null) {

            //calculates the positioning of the target and the missile
            var error = target.Position - Rigidbody.position;
            var errorDir = Quaternion.Inverse(Rigidbody.rotation) * error.normalized;

            if (error.magnitude <= lockRange && Vector3.Angle(Vector3.forward, errorDir) <= lockAngle) {
                MissileTracking = true;
                targetDir = errorDir;
            }
        }

        //pre determines the missile lock direction and the speed of the missile lock for missile to be immediately ready
        missileLockDirection = Vector3.RotateTowards(missileLockDirection, targetDir, Mathf.Deg2Rad * lockSpeed * dt, 0);
        MissileLocked = Target != null && MissileTracking && Vector3.Angle(missileLockDirection, targetDir) < lockSpeed * dt;
    }

    void UpdateCannon(float dt) 
    {
        //array for muzzle positions for future proofing for multiple cannons
        for(var i = 0; i < muzzle.Length; i += 1)
        {
            if (cannonFiring && cannonFiringTimer == 0 && ammunition > 0) 
            {
                cannonFiringTimer = 60f / cannonFireRate;
                //spread is calculated inside the unit circle and multiplied by the cannon spread
                var spread = UnityEngine.Random.insideUnitCircle * cannonSpread;

                //similar to the missile, the bullet is fired and the ammunition is reduced
                var bulletGO = Instantiate(bulletPrefab, muzzle[arrayPos].position, muzzle[arrayPos].rotation * Quaternion.Euler(spread.x, spread.y, 0));
                var bullet = bulletGO.GetComponent<Bullet>();
                bullet.Fire(this, cannonSpeed, muzzle[arrayPos]);
                Ammunition(-1);
            }
        }
        if (arrayPos >= muzzle.Length -1)
            arrayPos = 0;
        else 
            arrayPos += 1;
    }

    void Ammunition(float ammo)
    {
        //ammunition is added or subtracted from the ammunition count 
        ammunition += ammo;
        if (ammunition <= 0)
        {
            ammunition = 0;
            Debug.Log("Ammuniton is Empty");
        }
    }

    void UpdateFlare(float dt) 
    {
        //once again, similar to the armaments, the flare is fired and the flare timer is set, with similar fashion to everything else

        if (flareFiring && flareFiringTimer == 0) 
        {
            flareFiringTimer = 60f / flareFireRate;

            var spread = UnityEngine.Random.insideUnitSphere * flareSpread;
            var bulletGO = Instantiate(measuresPrefab, flarePoint.position, flarePoint.rotation * Quaternion.Euler(spread.x, spread.y, spread.z));
            var bullet = bulletGO.GetComponent<Flare>();
            bullet.Fire(this);
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        //draws gizmos in the editor to show the health and wireframe of the aircraft for debugging purposes
        Gizmos.color = Color.Lerp(Color.red, Color.green, 1000f);
        Gizmos.DrawWireMesh(this.GetComponent<MeshCollider>().sharedMesh, transform.position, transform.rotation, transform.localScale);

    }

    private void DamageController()
    {
        //future functionality for damage control and random failures
        var engineFailure = UnityEngine.Random.Range(0, 100);
        var hydraulicFailure = UnityEngine.Random.Range(0, 100);
        var electricalFailure = UnityEngine.Random.Range(0, 100);
        var fuelLeak = UnityEngine.Random.Range(0, 100);

        if (engineFailure <= engineFailureChance)
        {
            Debug.Log("Engine Failure");
            for (int i = 0; i < engines.Length; i++)
            {
                animation.EngineFailure(i);
                //remove one engine from the list 
                if (engines[i] != null)
                {
                    engines[i] = null;
                    break;
                }
            }
        }
    }

    void HealthController()
    {
        //all the joints will fly off if the fuselage health is 0 to give a more realistic feel
        if (fuselageHealth <= 0)
        {
            //break all joints attached to the fuselage
            var joints = this.gameObject.GetComponentsInChildren<Joint>();
            foreach (var joint in joints)
            {
                joint.breakForce = 0;
                joint.transform.parent = null;
                Instantiate(animation.explosionFX, transform.position, transform.rotation);
                Destroy(gameObject);
            }
        }
    }

    //All the functionality below is setting input for the aircraft
    public void SetThrottleInput(float input)
    {
        if (Dead) return;
        throttleInput = input;
    }

    public void SetBrakesInput(bool input)
    {
        if (Dead) return;
        wheelBrakes = input;
    }

    public void SetParkingBrake()
    {
        if (Dead) return;
        parkingBrakes = !parkingBrakes;
    }
    public void SetLandingGear()
    {
        if (Dead) return;
        landingGear = !landingGear;
    }

    public void SetControlInput(Vector3 input)
    {
        if (Dead) return;
        controlInput = Vector3.ClampMagnitude(input, 1);
    }

    public void SetCannonInput(bool input)
    {
        if (Dead) return;
        cannonFiring = input;
    }

    public void SetFlareInput(bool input)
    {
        if (Dead) return;
        flareFiring = input;
    }

    public void ToggleFlaps()
    {
        FlapsDeployed = !FlapsDeployed;
    }


    void FixedUpdate() {
        float dt = Time.fixedDeltaTime;

        UpdateThrottle(dt);
        UpdateThrust();
        FuelController(dt);
        UpdateWeapons(dt);
        HealthController();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.transform.root == this.gameObject.transform.root)
            UnityEngine.Physics.IgnoreCollision(collision.collider, this.gameObject.GetComponent<Collider>());
        else
        {
            Debug.Log(this.gameObject.name + " collided at a speed of " + collision.relativeVelocity.magnitude + " m/s");
            fuselageHealth -= collision.relativeVelocity.magnitude * collisionDamage;
        }
    }

    //ignore collisions with the gameobject's that this script is attached to
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.transform.root == this.gameObject.transform.root)
            UnityEngine.Physics.IgnoreCollision(collision.collider, this.gameObject.GetComponent<Collider>());
    }
}