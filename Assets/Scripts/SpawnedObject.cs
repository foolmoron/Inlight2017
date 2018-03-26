using UnityEngine;
using System.Collections;

public class SpawnedObject : MonoBehaviour {

    public ImageRecord Record;
    public bool ZAligned;
    Texture2D prevTex;

    [Range(0, 10)]
    public float ScaleFactor = 1;
    [Range(0, 10)]
    public float TargetScale = 1;
    [Range(0, 1)]
    public float ScaleSpeed = 0.9f;
    public bool UseScaleSpeed = true;
    public Transform ScaleTarget;
    
    public AnimationCurve DieCurve;
    public float LifeTime;
    float dieScaleFactor;

    Vector2 originalScale;
    new Renderer renderer;

    void Awake() {
        renderer = this.GetComponentInSelfOrChildren<Renderer>();
    }

    void Start() {
        ScaleTarget = ScaleTarget != null ? ScaleTarget : transform;
        originalScale = ScaleTarget.localScale;
        renderer.material.EnableKeyword("_EMISSION");
    }

    void OnObtain() {
        LifeTime = 0;
    }

    void FixedUpdate() {
        if (UseScaleSpeed) {
            ScaleFactor = Mathf.Lerp(ScaleFactor, TargetScale, ScaleSpeed);
        }
        LifeTime += Time.deltaTime;
        dieScaleFactor = DieCurve.Evaluate(LifeTime);
        if (dieScaleFactor <= 0) {
            gameObject.Release();
        }
    }

    void Update() {
        if (Record != null) {
            if (Record.Dimensions != Vector2.zero && !ZAligned)
                ScaleTarget.localScale = new Vector3(originalScale.x * Record.Dimensions.aspect(), originalScale.y, 1) * ScaleFactor * dieScaleFactor;
            if (Record.Dimensions != Vector2.zero && ZAligned)
                ScaleTarget.localScale = new Vector3(1, originalScale.y, originalScale.x * Record.Dimensions.aspect()) * ScaleFactor * dieScaleFactor;
            if (Record.Texture && Record.Texture != prevTex) {
                renderer.material.mainTexture = Record.Texture;
                renderer.material.mainTextureScale = Record.Facing == ImageFacing.Left ? Vector2.one : new Vector2(-1, 1);
                renderer.material.SetTexture("_EmissionMap", Record.Texture);
                renderer.material.SetColor("_EmissionColor", Color.white);
            }
            prevTex = Record.Texture;
        }
        if (Record == null || Record.Dimensions == Vector2.zero) {
            ScaleTarget.localScale = Vector3.one * ScaleFactor * dieScaleFactor;
        }
    }
}
