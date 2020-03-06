using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMotion : Manager<CameraMotion> {

    public Vector3 AngleRange = new Vector3(25, 25, 25);
    [Range(0, 0.01f)]
    public float RotationSpeed = 0.005f;
    [Range(0, 10)]
    public float RotationInterval = 2;
    [Range(0, 1)]
    public float RotationIntervalVariance = 0.5f;
    float timeToInterval;

    public Vector3 AngleRange2 = new Vector3(1, 1, 0);
    [Range(0, 1)]
    public float RotationInterval2 = 0.05f;
    [Range(0, 0.1f)]
    public float RotationIntervalVariance2 = 0.5f;
    float timeToInterval2;

    public Transform ZoomTarget;
    public Vector2 ZoomAmounts = new Vector2(13.1f, 34.3f);
    public Vector2 ZoomChances = new Vector2(0.1f, 0.5f);
    [Range(0, 0.1f)]
    public float ZoomSpeed = 0.05f;
    [Range(0, 100)]
    public float ZoomInterval = 10;
    float timeToZoom;

    Quaternion originalRotation;
    Quaternion targetRotation;
    Camera cam;

    void Start() {
        originalRotation = transform.rotation;
        cam = GetComponent<Camera>();
    }

    void Update() {
        timeToInterval -= Time.deltaTime;
        if (timeToInterval <= 0) {
            timeToInterval = RotationInterval * (1 + (Random.value - 0.5f) * RotationIntervalVariance);
            targetRotation = originalRotation * Quaternion.Euler(new Vector3((Random.value - 0.5f) * AngleRange.x, (Random.value - 0.5f) * AngleRange.y, (Random.value - 0.5f) * AngleRange.z));
        }
        timeToInterval2 -= Time.deltaTime;
        if (timeToInterval2 <= 0) {
            timeToInterval2 = RotationInterval2 * (1 + (Random.value - 0.5f) * RotationIntervalVariance2);
            targetRotation = targetRotation * Quaternion.Euler(new Vector3((Random.value - 0.5f) * AngleRange2.x, (Random.value - 0.5f) * AngleRange2.y, (Random.value - 0.5f) * AngleRange2.z));
        }
        if (ZoomTarget) {
            targetRotation = Quaternion.LookRotation(ZoomTarget.position - transform.position);
        }
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed);

        timeToZoom -= Time.deltaTime;
        if (timeToZoom <= 0) {
            timeToZoom = ZoomInterval;
            if (Random.value < (ZoomTarget != null ? ZoomChances.y : ZoomChances.x)) {
                ZoomTarget = ZoomTarget != null 
                    ? null 
                    : SpawnedObject.AllInCurrentScene.RandomWhere(o => o.gameObject.activeSelf).transform
                    ;
            }
        }
        var targetSize = ZoomTarget != null ? ZoomAmounts.x : ZoomAmounts.y;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, ZoomSpeed);
    }
}