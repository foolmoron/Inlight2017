using UnityEngine;
using System.Collections;

public class SpawnedObject : MonoBehaviour {

    public MaterialPropertyBlock Properties { get; private set; }
    
    public bool ZAligned;

    [Range(0, 10)]
    public float ScaleFactor = 1;
    [Range(0, 10)]
    public float TargetScale = 1;
    [Range(0, 1)]
    public float ScaleSpeed = 0.9f;
    public bool UseScaleSpeed = true;
    public Transform ScaleTarget;

    Vector2 originalScale;
    HasImageRecord record;
    new Renderer renderer;

    Texture2D prevTex;
    float prevScale;
    int framesWithNoChange = 0;

    void Awake() {
        record = GetComponent<HasImageRecord>();
        renderer = this.GetComponentInSelfOrChildren<Renderer>();
        Properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(Properties);
    }

    void Start() {
        ScaleTarget = ScaleTarget != null ? ScaleTarget : transform;
        originalScale = ScaleTarget.localScale;
        Update();
    }

    void FixedUpdate() {
        if (UseScaleSpeed) {
            ScaleFactor = Mathf.Lerp(ScaleFactor, TargetScale, ScaleSpeed);
        }
        framesWithNoChange++;
        if (Mathf.Abs(ScaleFactor - prevScale) > 0.0001f) {
            framesWithNoChange = 0;
        }
        prevScale = ScaleFactor;
        if (framesWithNoChange > 10) {
            enabled = false;
        }
    }

    void Update() {
        if (record.Record != null) {
            if (record.Record.Dimensions != Vector2.zero && !ZAligned)
                ScaleTarget.localScale = new Vector3(originalScale.x * record.Record.Dimensions.aspect(), originalScale.y, 1) * ScaleFactor;
            if (record.Record.Dimensions != Vector2.zero && ZAligned)
                ScaleTarget.localScale = new Vector3(1, originalScale.y, originalScale.x * record.Record.Dimensions.aspect()) * ScaleFactor;
            if (record.Record.Texture && record.Record.Texture != prevTex) {
                var material = ImageReader.MaterialsCache.Get(record.Record.Texture);
                if (material == null) {
                    material = new Material(renderer.material) {
                        mainTexture = record.Record.Texture,
                        mainTextureScale = new Vector2(record.Record.Facing == ImageFacing.Left ? 1 : -1, 1)
                    };
                    ImageReader.MaterialsCache[record.Record.Texture] = material;
                }
                renderer.material = material;
                renderer.SetPropertyBlock(Properties);
                prevTex = record.Record.Texture;
            }
        }
        if (record.Record == null || record.Record.Dimensions == Vector2.zero) {
            ScaleTarget.localScale = Vector3.one * ScaleFactor;
        }
    }
}
