using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using UnityEngine;


/*experimental functionality to provide a more realistic engine startup using numbers to simulate engine spooling and correct button procedures
however this was unfinished as there was no time to implement it into the plane as there is no cockpit to display the information
*/
public class Systems : MonoBehaviour
{
    [Header("Diagnostics"), Tooltip("Engine RPM is rounded to tens")]
    public float engineRPM;
    public string enginePercent;
    
    [Header("Statistics")]
    public float duration;
    public float maxRPM;
    public AnimationCurve spoolCurve;

    [Header("Buttons")]
    public bool jetFuelStarter;
    public bool engineStarterA;
    public bool engineStarterB;
    public bool fuelPumpA;
    public bool fuelPumpB;

    [Header("References")]
    public Plane script;

    float starterTime;
    float timestepA;
    float timestepB;
    
    float spool;
    double percentage;
    bool spooling;
    bool engineReady;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Display();
        Starter();
    }

    void Starter()
    {
        //The player must press the jet fuel starter button to start the engine and supply it with adequate power
        if(jetFuelStarter && !spooling)
        {
            starterTime += Time.deltaTime;
            spool = Mathf.SmoothStep(spool, maxRPM / 4f, spoolCurve.Evaluate(starterTime / duration));
        }

        //one engine functionality to create a engineStarter button to start the engine, with fuel pumops as well
        if(percentage > 24 && engineStarterA && fuelPumpA && !engineReady)
        {
            spooling = true;
            timestepA += Time.deltaTime;
            spool = Mathf.SmoothStep(spool, maxRPM / 1.5f, spoolCurve.Evaluate(timestepA / duration));
            if(percentage > 66)
                engineReady = true;
        }

        if(engineReady)
        {

        }


    }




    void Display()
    {
        percentage = (engineRPM / maxRPM) * 100f;
        percentage = System.Math.Round(percentage,1);
        engineRPM = spool;
        engineRPM = (int)engineRPM;

        enginePercent = percentage + " %";
    }
}
