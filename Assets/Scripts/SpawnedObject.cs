using UnityEngine;
using System.Collections;

public class SpawnedObject : MonoBehaviour {

    public ImageRecord Record;

    Vector2 originalScale;

    void Awake() {
        originalScale = transform.localScale;
    }

    void Update() {
        if (Record != null && Record.Dimensions != Vector2.zero) {
            transform.localScale = new Vector3(originalScale.x * Record.Dimensions.aspect(), originalScale.y, 1);
        }
    }
}
