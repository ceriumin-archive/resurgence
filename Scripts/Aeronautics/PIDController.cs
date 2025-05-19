using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class PIDController {

    /* PID control is a well-established way of driving a system towards a target position or control parameters, it works by calculating the error
    from the current value and target value and then applying a correction to the system based on the error. The correction is made up of three terms,
    however we do not need to work out the third deriative term. It is useful to prevent aircraft overacceleration
    */

    //PID coefficients
    [Header("PID Terms")]
    [SerializeField] public float proportionalGain;
    [SerializeField] public float integralGain;
    [SerializeField] public float derivativeGain;

    [Header("Output Terms")]
    [SerializeField] public float outputMin = -1;
    [SerializeField] public float outputMax = 1;
    [SerializeField] public float integralSaturation;

    [Header("Derivative Terms")]
    [SerializeField] public float valueLast;
    [SerializeField] public float errorLast;
    [SerializeField] public float integrationStored;
    [SerializeField] public bool derivativeInitialized;

    public float Update(float dt, float currentValue, float targetValue) {
        if (dt <= 0) throw new ArgumentOutOfRangeException(nameof(dt));

        float error = targetValue - currentValue;

        //calculate the proportional term
        float P = proportionalGain * error;

        //calculate the Integral ter,
        integrationStored = Mathf.Clamp(integrationStored + (error * dt), -integralSaturation, integralSaturation);
        float I = integralGain * integrationStored;

        //calculate the Deriative term
        errorLast = error;
        valueLast = currentValue;
        //choose D term to use
        float deriveMeasure = 0;

        float D = derivativeGain * deriveMeasure;

        //adds all the terms together to get the output value
        float result = P + I + D;
        return Mathf.Clamp(result, outputMin, outputMax);
    }

}
