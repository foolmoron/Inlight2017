using UnityEngine;
using System.Collections;

public class SpawnedObject : MonoBehaviour {

    public MaterialPropertyBlock Properties { get; private set; }
    
    public bool ZAligned;

    [Range(0, 10)]
    public float ScaleFactor = 1;
    [Range(0, 10)]
    public float TargetScale = 1;
    [Range(0, 0.5f)]
    public float ScaleSpeed = 0.2f;
    public bool UseScaleSpeed = true;
    public Transform ScaleTarget;

    public Material TallMaterial;
    public Material LongMaterial;
    public Material WiggleMaterial;

    public bool IsWiggling;
    bool prevWiggling;

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

    public void Update() {
        if (record.Record != null) {
            if (record.Record.Dimensions != Vector2.zero && !ZAligned)
                ScaleTarget.localScale = new Vector3(originalScale.x * record.Record.Dimensions.aspect(), originalScale.y, 1) * ScaleFactor;
            if (record.Record.Dimensions != Vector2.zero && ZAligned)
                ScaleTarget.localScale = new Vector3(1, originalScale.y, originalScale.x * record.Record.Dimensions.aspect()) * ScaleFactor;
            var shouldSetMaterial = false;
            if (record.Record.Texture && record.Record.Texture != prevTex) {
                var mats = ImageReader.MaterialsCache.Get(record.Record.Texture);
                if (mats == null) {
                    mats = new MaterialSet {
                        TallMaterial = new Material(TallMaterial),
                        LongMaterial = new Material(LongMaterial),
                        WiggleMaterial = new Material(WiggleMaterial),
                    };
                    mats.SetTexture(record.Record.Texture);
                    mats.SetFlip(record.Record.Facing == ImageFacing.Left);
                    ImageReader.MaterialsCache[record.Record.Texture] = mats;
                }
                shouldSetMaterial = true;
                prevTex = record.Record.Texture;
            }
            shouldSetMaterial |= prevWiggling != IsWiggling;
            if (shouldSetMaterial) {
                var mats = ImageReader.MaterialsCache.Get(record.Record.Texture);
                renderer.material = IsWiggling ? mats.WiggleMaterial : record.Record.IsTall ? mats.TallMaterial : mats.LongMaterial;
                renderer.SetPropertyBlock(Properties);
            }
            prevWiggling = IsWiggling;
        }
        if (record.Record == null || record.Record.Dimensions == Vector2.zero) {
            ScaleTarget.localScale = Vector3.one * ScaleFactor;
        }
    }
}
