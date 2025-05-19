using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyCenterOfMass : MonoBehaviour
{
    //part of a series of scripts to set the center of mass of a rigidbody. It provides a visual representation of the center of mass in the editor.
    [Header("Rigidbody Center of Gravity")]
    [SerializeField] public Vector3 CenterOfMass = new Vector3(0, 0, 0);

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _rb.centerOfMass = CenterOfMass;
    }
}
