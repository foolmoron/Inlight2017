using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Egg : MonoBehaviour
{
    public GameObject AnimalPrefab;
    ObjectPool animalPool;

    [Range(0, 5)]
    public int Health = 3;

    [Range(0, 10)]
    public float AnimalOriginalScale = 0.2f;
    [Range(0, 10)]
    public float AnimalFinalScaleMin = 1f;
    [Range(0, 10)]
    public float AnimalFinalScaleMax = 4f;
    [Range(0, 1)]
    public float AnimalScaleSpeed = 0.2f;

    [Range(0, 300)]
    public float DeathTime = 180;
    float deathTime;

    public ImageRecord Record;
    GameObject solid;
    List<GameObject> shards;

    void Awake() {
        solid = transform.parent.Find("pSolid1").gameObject;
        shards = new List<GameObject> {
            transform.parent.Find("shard1_surfaceShatter").gameObject,
            transform.parent.Find("surfaceShatter_shard2").gameObject,
            transform.parent.Find("surfaceShatter_shard3").gameObject,
            transform.parent.Find("surfaceShatter_shard4").gameObject,
            transform.parent.Find("surfaceShatter_shard5").gameObject,
        };
    }

    void Start() {
        animalPool = AnimalPrefab.GetObjectPool(100);
    }

    void Update() {
        if (Record == null) {
            Record = ImageReader.Inst.GetWeightedRandomAnimal();
        }
        if (Record != null) {
            solid.GetComponent<Renderer>().material.color = Record.MainColor;
            foreach (var shard in shards) {
                shard.GetComponent<Renderer>().material.color = Record.MainColor;
            }
        }
        deathTime += Time.deltaTime;
        if (deathTime >= DeathTime) {
            Destroy(transform.parent.gameObject);
        }
    }

    void BreakShard(GameObject shard) {
        shard.transform.parent = null;
        shard.GetComponent<Collider>().enabled = true;
        shard.GetComponent<Rigidbody>().isKinematic = false;
        shard.GetComponent<Animator>().enabled = false;
        shard.GetComponent<DieOverTime>().enabled = true;
    }
    
    void OnTriggerEnter(Collider other) {
        var shard = shards.Random();
        shards.Remove(shard);
        foreach (var s in shards) {
            s.GetComponent<Animator>().enabled = true;
        }
        BreakShard(shard);

        Health--; 
        if (Health <= 0) {
            var animal = animalPool.Obtain<SpawnedObject>(transform.parent.position);
            animal.Record = Record;
            animal.ScaleFactor = AnimalOriginalScale;
            animal.TargetScale = Mathf.Lerp(AnimalFinalScaleMin, Single.MaxValue, Random.value);
            animal.ScaleSpeed = AnimalScaleSpeed;
            animal.GetComponentInSelfOrChildren<Wander>().enabled = true;
            animal.GetComponentInChildren<Animation>().enabled = true;

            foreach (var s in shards) {
                BreakShard(s);
            }
            Destroy(transform.parent.gameObject);
        }
    }
}