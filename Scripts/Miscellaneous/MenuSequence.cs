using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuSequence : MonoBehaviour
{
    public bool isPlaying;
    public Light spotlight;
    public Image title;
    public Transform start;

    public float timeToWait;
    public float titleWait;
    // Start is called before the first frame update
    void Start()
    {
        spotlight.intensity = 0f;
        title.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.Space))
        {
            isPlaying = true;
        }

        if(isPlaying)
        {
            timeToWait -= Time.deltaTime;
            start.gameObject.SetActive(false);
            if (timeToWait <= 0)
            {
                titleWait -= Time.deltaTime;
                spotlight.intensity = Mathf.Lerp(spotlight.intensity, 500, Time.deltaTime);
                if (titleWait <= 0)
                {
                    title.gameObject.SetActive(true);
                }

            }
        }
    }
}
