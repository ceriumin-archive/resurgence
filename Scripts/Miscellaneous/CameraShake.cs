using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//revamped camera shake script for the purpose of the game
public class CameraShake : MonoBehaviour
{
    Vector3 originalPos;
    float lerpedMagnitude;

    void Start()
    {
        originalPos = transform.localPosition;
    }

    //Toggleable function for camera shake to be toggled
    public void Shake(bool isActive, float magnitude)
    {
        magnitude /= 1000f;

        //randomly moves the camera within a certain range multiplied by a magnitude for intensity
        if(isActive)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(x, y, originalPos.z);
        }

        if(!isActive)
            transform.localPosition = originalPos;
        
    }

    //Function to shake the camera for a certain amount of time invoked through a coroutine 
    public IEnumerator ShakeTime(float duration, float magnitude)
    {
        float elapsed = 0.0f;
        while(elapsed < duration)
        {
            lerpedMagnitude = Mathf.Lerp(magnitude, 0f, (elapsed / duration));
            float x = Random.Range(-1f, 1f) * lerpedMagnitude;
            float y = Random.Range(-1f, 1f) * lerpedMagnitude;

            transform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos, 0.1f); 
    }
    

}
