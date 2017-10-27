using UnityEngine;
using System.Collections;

public class SpawnedObject : MonoBehaviour {

    public ImageRecord Record;
    [Range(0, 10)]
    public float ScaleFactor = 1;
    public bool ZAligned;

    Vector2 originalScale;
    new Renderer renderer;

    void Awake() {
        originalScale = transform.localScale;
        renderer = this.GetComponentInSelfOrChildren<Renderer>();
    }

    void Update() {
        if (Record != null) {
            if (Record.Dimensions != Vector2.zero && !ZAligned)
                transform.localScale = new Vector3(originalScale.x * Record.Dimensions.aspect(), originalScale.y, 1) * ScaleFactor;
            if (Record.Dimensions != Vector2.zero && ZAligned)
                transform.localScale = new Vector3(1, originalScale.y, originalScale.x * Record.Dimensions.aspect()) * ScaleFactor;
            if (Record.Texture)
                renderer.material.mainTexture = Record.Texture;
        }
    }
}
