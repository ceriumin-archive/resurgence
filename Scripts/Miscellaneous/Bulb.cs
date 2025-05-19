using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//General script for a lightbulb, can be used for any lightbulb in the game
public class Bulb : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public Light _light;
    [SerializeField] public Material mat;

    [Header("Settings")]
    [SerializeField] public bool lightOn;

    [Header("Strobe Lights")]
    [SerializeField] bool strobeLight;
    [SerializeField] float strobeSpeed;

    private float i;

    void Start()
    {
        i = _light.intensity;
    }

    // Update is called once per frame
    void Update()
    {
        //turns the texture emission on and off, to simulate the light glowing, and lerps the values to make it smooth
        if (lightOn)
        {
            _light.intensity = Mathf.Lerp(_light.intensity, i, 0.1f);
            mat.EnableKeyword("_EMISSION");
        }

        else
        {
            _light.intensity = Mathf.Lerp(_light.intensity, 0, 0.1f);
            mat.DisableKeyword("_EMISSION");
        }
    }
}
