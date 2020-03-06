using UnityEngine;
using System.Collections;

/// <summary>
/// Creates wandering behaviour for a CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class Wander : MonoBehaviour
{
    public float speed = 5;
    public float directionChangeInterval = 1;
    public float directionChangeTime = 0;
    public float maxHeadingChange = 30;

    [Range(0, 1)]
    public float HeadingFlipChange = 0.95f;
    [Range(0, 20)]
    public float NoHeadingChangeTime = 10f;
    float noHeadingTime;

    SpawnedObject spawned;
    CharacterController controller;
    float heading;
    Vector3 targetRotation;

    void Awake()
    {
        spawned = GetComponent<SpawnedObject>();
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (noHeadingTime > 0) {
            noHeadingTime -= Time.deltaTime;
        }
        directionChangeTime -= Time.deltaTime;
        if (directionChangeTime <= 0 && noHeadingTime <= 0) {
            directionChangeTime = directionChangeInterval;
            var floor = Mathf.Clamp(heading - maxHeadingChange, 0, 360);
            var ceil = Mathf.Clamp(heading + maxHeadingChange, 0, 360);
            heading = Random.Range(floor, ceil);
            targetRotation = Quaternion.AngleAxis(heading, Vector3.up).eulerAngles;
        }
        
        transform.eulerAngles = Vector3.Slerp(transform.eulerAngles, targetRotation, Time.deltaTime * directionChangeInterval);
        var forward = transform.TransformDirection(Vector3.forward);
        if (!spawned.IsWiggling) {
            controller.SimpleMove(forward * speed);
        }
    }

    void OnTriggerEnter(Collider other) {
        if (Random.value < HeadingFlipChange) {
            heading = (heading + 180f) % 360f;
            targetRotation = Quaternion.AngleAxis(heading, Vector3.up).eulerAngles;
            noHeadingTime = NoHeadingChangeTime;
        }
    }

}