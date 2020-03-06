using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnedObject : MonoBehaviour {

    public static List<SpawnedObject> AllInCurrentScene = new List<SpawnedObject>();

    public MaterialPropertyBlock Properties { get; private set; }
    
    public bool ZAligned;

    [Range(0, 1200)]
    public float DeathTime = 240;

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

    ParticleSystem glimmerParticles;
    public bool IsGlimmering;

    Vector2 originalScale;
    public HasImageRecord Record { get; private set; }
    new Renderer renderer;
    DieOverTime dieOverTime;

    Texture2D prevTex;
    float prevScale;
    int framesWithNoChange = 0;

    void Awake() {
        AllInCurrentScene.Add(this);
        Record = GetComponent<HasImageRecord>();
        dieOverTime = GetComponent<DieOverTime>();
        dieOverTime.enabled = false;
        renderer = this.GetComponentInSelfOrChildren<Renderer>();
        Properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(Properties);
        glimmerParticles = this.GetComponentInChildren<ParticleSystem>();
        glimmerParticles.enableEmission(false);
    }

    void Start() {
        ScaleTarget = ScaleTarget != null ? ScaleTarget : transform;
        originalScale = ScaleTarget.localScale;
        Update();
    }

    void OnObtain() {
        enabled = true;
        IsWiggling = false;
        IsGlimmering = false;
        dieOverTime.enabled = false;
        dieOverTime.LifeTime = 0;
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
            EventManager.Inst.Delay(Die, DeathTime);
        }
    }

    void Die() {
        dieOverTime.enabled = true;
    }

    public void Update() {
        if (Record.Record != null) {
            if (Record.Record.Dimensions != Vector2.zero && !ZAligned)
                ScaleTarget.localScale = new Vector3(originalScale.x * Record.Record.Dimensions.aspect(), originalScale.y, 1) * ScaleFactor;
            if (Record.Record.Dimensions != Vector2.zero && ZAligned)
                ScaleTarget.localScale = new Vector3(1, originalScale.y, originalScale.x * Record.Record.Dimensions.aspect()) * ScaleFactor;
            var shouldSetMaterial = false;
            if (Record.Record.Texture && Record.Record.Texture != prevTex) {
                var mats = ImageReader.MaterialsCache.Get(Record.Record.Texture);
                if (mats == null) {
                    mats = new MaterialSet {
                        TallMaterial = new Material(TallMaterial),
                        LongMaterial = new Material(LongMaterial),
                        WiggleMaterial = new Material(WiggleMaterial),
                    };
                    mats.SetTexture(Record.Record.Texture);
                    mats.SetFlip(Record.Record.Facing == ImageFacing.Left);
                    ImageReader.MaterialsCache[Record.Record.Texture] = mats;
                }
                shouldSetMaterial = true;
                prevTex = Record.Record.Texture;
            }
            shouldSetMaterial |= prevWiggling != IsWiggling;
            if (shouldSetMaterial) {
                var mats = ImageReader.MaterialsCache.Get(Record.Record.Texture);
                var isWiggling = IsWiggling || ImageReader.Inst.ALWAYS_WIGGLE;
                renderer.material = isWiggling ? mats.WiggleMaterial : Record.Record.IsTall ? mats.TallMaterial : mats.LongMaterial;
                renderer.SetPropertyBlock(Properties);
            }
            prevWiggling = IsWiggling;

            glimmerParticles.enableEmission(IsGlimmering);
            var gpcol = glimmerParticles.colorOverLifetime;
            var gpcolc = gpcol.color;
            var gpcolcg = gpcolc.gradient;
            var gpcolcgc = gpcolc.gradient.colorKeys;
            gpcolcgc[0].color = Record.Record.Color2;
            gpcolcgc[1].color = Record.Record.Color1;
            gpcolcgc[2].color = Record.Record.Color2;
            gpcolcgc[3].color = Record.Record.Color1;
            gpcolcg.colorKeys = gpcolcgc;
            gpcolc.gradient = gpcolcg;
            gpcol.color = gpcolc;
        }
        if (Record.Record == null || Record.Record.Dimensions == Vector2.zero) {
            ScaleTarget.localScale = Vector3.one * ScaleFactor;
        }
    }

    void OnDestroy() {
        AllInCurrentScene.Remove(this);
    }
}
