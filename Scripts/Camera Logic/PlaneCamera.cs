using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneCamera : MonoBehaviour {
    [SerializeField]
    new Camera camera;
    [SerializeField]
    Transform CameraHolder;
    [SerializeField]
    float Sensitivity = 200f;


    float xAxisRotation = 0f;
    float yAxisRotation = 0f;
    Transform cameraTransform;
    Plane plane;
    Transform planeTransform;
    Vector2 lookInput;

    void Awake() {
        cameraTransform = camera.GetComponent<Transform>();
        cameraTransform.SetParent(CameraHolder);
    }

    public void SetInput(Vector2 input) {
        lookInput = input;
    }

    void FixedUpdate() {

        float X = lookInput.x * Sensitivity * Time.deltaTime;
        float Y = lookInput.y * Sensitivity * Time.deltaTime;

        yAxisRotation -= Y;
        yAxisRotation = Mathf.Clamp(yAxisRotation, -90f, 50f);
        xAxisRotation -= X;
        xAxisRotation = Mathf.Clamp(xAxisRotation, -120f, 120f);

        cameraTransform.localRotation = Quaternion.Euler(yAxisRotation, -xAxisRotation, 0f);
    }

}
