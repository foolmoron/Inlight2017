﻿using UnityEngine;
using System.Collections;

public class SpawnedObject : MonoBehaviour {

    public MaterialPropertyBlock Properties { get; private set; }

    public ImageRecord Record;
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
    new Renderer renderer;

    Texture2D prevTex;
    float prevScale;
    int framesWithNoChange = 0;

    void Awake() {
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
        if (Record != null) {
            if (Record.Dimensions != Vector2.zero && !ZAligned)
                ScaleTarget.localScale = new Vector3(originalScale.x * Record.Dimensions.aspect(), originalScale.y, 1) * ScaleFactor;
            if (Record.Dimensions != Vector2.zero && ZAligned)
                ScaleTarget.localScale = new Vector3(1, originalScale.y, originalScale.x * Record.Dimensions.aspect()) * ScaleFactor;
            if (Record.Texture && Record.Texture != prevTex) {
                var material = ImageReader.MaterialsCache.Get(Record.Texture);
                if (material == null) {
                    material = new Material(renderer.material) {
                        mainTexture = Record.Texture,
                        mainTextureScale = new Vector2(Record.Facing == ImageFacing.Left ? 1 : -1, 1)
                    };
                    ImageReader.MaterialsCache[Record.Texture] = material;
                }
                renderer.material = material;
                renderer.SetPropertyBlock(Properties);
                prevTex = Record.Texture;
            }
        }
        if (Record == null || Record.Dimensions == Vector2.zero) {
            ScaleTarget.localScale = Vector3.one * ScaleFactor;
        }
    }
}
