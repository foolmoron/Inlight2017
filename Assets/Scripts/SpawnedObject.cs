using UnityEngine;
using System.Collections;

public class SpawnedObject : MonoBehaviour {

    public ImageRecord Record;
    public bool ZAligned;

    [Range(0, 10)]
    public float ScaleFactor = 1;
    [Range(0, 10)]
    public float TargetScale = 1;
    [Range(0, 1)]
    public float ScaleSpeed = 0.9f;
    public Transform ScaleTarget;

    Vector2 originalScale;
    new Renderer renderer;

    void Awake() {
        renderer = this.GetComponentInSelfOrChildren<Renderer>();
    }

    void Start() {
        ScaleTarget = ScaleTarget != null ? ScaleTarget : transform;
        originalScale = ScaleTarget.localScale;
    }

    void FixedUpdate() {
        ScaleFactor = Mathf.Lerp(ScaleFactor, TargetScale, ScaleSpeed);
    }

    void Update() {
        if (Record != null) {
            if (Record.Dimensions != Vector2.zero && !ZAligned)
                ScaleTarget.localScale = new Vector3(originalScale.x * Record.Dimensions.aspect(), originalScale.y, 1) * ScaleFactor;
            if (Record.Dimensions != Vector2.zero && ZAligned)
                ScaleTarget.localScale = new Vector3(1, originalScale.y, originalScale.x * Record.Dimensions.aspect()) * ScaleFactor;
            if (Record.Texture)
                renderer.material.mainTexture = Record.Texture;
        }
        if (Record == null || Record.Dimensions == Vector2.zero) {
            ScaleTarget.localScale = Vector3.one * ScaleFactor;
        }
    }
}
