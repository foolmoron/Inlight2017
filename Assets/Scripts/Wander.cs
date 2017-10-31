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

    CharacterController controller;
    float heading;
    Vector3 targetRotation;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        directionChangeTime -= Time.deltaTime;
        if (directionChangeTime <= 0) {
            directionChangeTime = directionChangeInterval;
            var floor = Mathf.Clamp(heading - maxHeadingChange, 0, 360);
            var ceil = Mathf.Clamp(heading + maxHeadingChange, 0, 360);
            heading = Random.Range(floor, ceil);
            targetRotation = Quaternion.AngleAxis(heading, Vector3.up).eulerAngles;
        }
        
        transform.eulerAngles = Vector3.Slerp(transform.eulerAngles, targetRotation, Time.deltaTime * directionChangeInterval);
        var forward = transform.TransformDirection(Vector3.forward);
        controller.SimpleMove(forward * speed);
    }
}