﻿using UnityEngine;
using System.Collections;

public class SpawnOnGround : MonoBehaviour {

    public GameObject QuadPrefab;

    public LayerMask CollisionLayers;
    public float HeightOffsetFromGround;

    public Transform TargetTransform;
    public Vector3 PositionOffset;
    public Vector3 PositionRandomness;
    public Vector3 RotationRandomness;

    void Start() {
        ImageReader.Inst.OnAdded += record => {
            var quad = Instantiate(QuadPrefab.gameObject);
            quad.GetComponentInSelfOrChildren<Renderer>().material.mainTexture = record.Texture;

            var startPosition =
                TargetTransform.position +
                PositionOffset +
                new Vector3((Random.value - 0.5f) * PositionRandomness.x, (Random.value - 0.5f) * PositionRandomness.y, (Random.value - 0.5f) * PositionRandomness.z)
                ;
            RaycastHit hit;
            Physics.Raycast(startPosition, Vector3.down, out hit, 100, CollisionLayers.value);
            quad.transform.position = hit.point.plusY(HeightOffsetFromGround);

            quad.transform.rotation =
                Quaternion.Euler((Random.value - 0.5f) * RotationRandomness.x, (Random.value - 0.5f) * RotationRandomness.y, (Random.value - 0.5f) * RotationRandomness.z)
                ;
        };
    }
}
