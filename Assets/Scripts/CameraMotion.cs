using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMotion : MonoBehaviour {

    public Vector3 AngleRange = new Vector3(25, 25, 25);
    [Range(0, 0.01f)]
    public float RotationSpeed = 0.005f;
    [Range(0, 10)]
    public float RotationInterval = 2;
    [Range(0, 1)]
    public float RotationIntervalVariance = 0.5f;
    float timeToInterval;

    Quaternion originalRotation;
    Quaternion targetRotation;

    void Start() {
        originalRotation = transform.rotation;
    }

    void Update() {
        timeToInterval -= Time.deltaTime;
        if (timeToInterval <= 0) {
            timeToInterval = RotationInterval * (1 + (Random.value - 0.5f) * RotationIntervalVariance);
            targetRotation = originalRotation * Quaternion.Euler(new Vector3((Random.value - 0.5f) * AngleRange.x, (Random.value - 0.5f) * AngleRange.y, (Random.value - 0.5f) * AngleRange.z));
        }
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed);
    }
}