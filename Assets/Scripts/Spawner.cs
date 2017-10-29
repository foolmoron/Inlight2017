﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour {

    public GameObject SeedPrefab;
    ObjectPool seedPool;

    public GameObject EggPrefab;
    ObjectPool eggPool;

    List<SpawnedObject> objects = new List<SpawnedObject>(100);

    public LayerMask CollisionLayers;
    public float HeightOffsetFromGround;

    public Transform TargetTransform;
    public Vector3 PositionOffset;
    public Vector3 PositionRandomness;
    public Vector3 RotationRandomness;

    [Range(0, 20)]
    public float SpawnTimer;
    [Range(0, 20)]
    public float SpawnInterval = 4;
    [Range(0, 20)]
    public float SpawnIntervalRandomness = 2;

    void Start() {
        seedPool = SeedPrefab.GetObjectPool(20);
        eggPool = EggPrefab.GetObjectPool(20);

        // kill objects on image removed
        ImageReader.Inst.OnRemoved += record => {
            for (int i = 0; i < objects.Count; i++) {
                if (objects[i].Record == record) {
                    objects[i].gameObject.Release();
                    objects.RemoveAt(i);
                    i--;
                }
            }
        };
    }

    void Update() {
        // spawn new objects
        {
            SpawnTimer += Time.deltaTime;
            if (SpawnTimer >= SpawnInterval) {
                Spawn(ImageReader.Inst.GetWeightedRandomRecord());
                SpawnTimer = Random.value * SpawnIntervalRandomness;
            }
        }
    }
    
    void Spawn(ImageRecord record) {
        var obj = (record.Type == ImageType.Animal ? eggPool : seedPool).Obtain();
        objects.Add(obj.GetComponent<SpawnedObject>());

        var egg = obj.GetComponentInSelfOrChildren<Egg>();
        if (egg) {
            egg.Record = record;
        }
        var seed = obj.GetComponentInSelfOrChildren<Seed>();
        if (seed) {
            seed.Record = record;
        }

        var startPosition =
            TargetTransform.position +
            PositionOffset +
            new Vector3((Random.value - 0.5f) * PositionRandomness.x, (Random.value - 0.5f) * PositionRandomness.y, (Random.value - 0.5f) * PositionRandomness.z)
            ;
        RaycastHit hit;
        Physics.Raycast(startPosition, Vector3.down, out hit, 100, CollisionLayers.value);
        obj.transform.position = hit.point.plusY(HeightOffsetFromGround);

        obj.transform.rotation =
            Quaternion.Euler((Random.value - 0.5f) * RotationRandomness.x, (Random.value - 0.5f) * RotationRandomness.y, (Random.value - 0.5f) * RotationRandomness.z)
            ;
    }
}
