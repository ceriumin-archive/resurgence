using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroy : MonoBehaviour
{
    //General handling of particle systems to remove them after they have finished playing in the editor

    private ParticleSystem fx;
    // Start is called before the first frame update
    void Start()
    {
        fx = gameObject.GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        Destroy(gameObject, fx.duration + 1);
    }
}
