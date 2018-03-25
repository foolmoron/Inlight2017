using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class AnimalParams {
    [Range(1, 25)]
    public int MinCount = 1;
    [Range(1, 25)]
    public int MaxCount = 1;
    [Range(0, 10)]
    public float MinScaleStart;
    [Range(0, 10)]
    public float MaxScaleStart;
    [Range(0, 10)]
    public float MinScaleEnd;
    [Range(0, 10)]
    public float MaxScaleEnd;
    [Range(0, 30)]
    public float MinScaleDuration;
    [Range(0, 30)]
    public float MaxScaleDuration;
    [Range(0, 10)]
    public float MinSpeed;
    [Range(0, 10)]
    public float MaxSpeed;
    [Range(0, 5)]
    public float MinAnimSpeed;
    [Range(0, 5)]
    public float MaxAnimSpeed;
}

public class Egg : MonoBehaviour
{
    public GameObject AnimalPrefab;
    ObjectPool animalPool;

    [Range(0, 5)]
    public int Health = 3;
    [Range(0, 300)]
    public float DeathTime = 180;
    float deathTime;

    public AnimalParams SwarmParams;
    public AnimalParams GroupParams;
    public AnimalParams MonsterParams;

    public ImageRecord Record;
    GameObject solid;
    List<GameObject> shards;

    SemiDrag drag;

    void Awake() {
        drag = transform.parent.GetComponent<SemiDrag>();
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
        solid.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
        foreach (var shard in shards) {
            shard.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
        }
    }

    void Update() {
        if (Record == null) {
            Record = ImageReader.Inst.GetWeightedRandomAnimal();
        }
        if (Record != null) {
            solid.GetComponent<Renderer>().material.color = Record.MainColor;
            solid.GetComponent<Renderer>().material.SetColor("_EmissionColor", Record.MainColor);
            foreach (var shard in shards) {
                shard.GetComponent<Renderer>().material.color = Record.MainColor;
                shard.GetComponent<Renderer>().material.SetColor("_EmissionColor", Record.MainColor);
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

        if (other) {
            drag.GetTouched(other.transform.position, other.transform.forward);
        }

        Health--; 
        if (Health <= 0) {
            var r = Random.value;
            var animalParams =
                r < 0.15f ? MonsterParams :
                r < 0.5f ? GroupParams :
                SwarmParams
                ;

            var count = Mathf.FloorToInt(Mathf.Lerp(animalParams.MinCount, animalParams.MaxCount, Random.value));
            for (int i = 0; i < count; i++) {
                var animal = animalPool.Obtain<SpawnedObject>(transform.parent.position);
                animal.Record = Record;
                animal.UseScaleSpeed = false;

                var animalScaled = animal.GetComponent<SmoothScaler>();
                var scaleLerp = Random.value;
                animalScaled.OriginalScale = Mathf.Lerp(animalParams.MinScaleStart, animalParams.MaxScaleStart, scaleLerp);
                animalScaled.TargetScale = Mathf.Lerp(animalParams.MinScaleEnd, animalParams.MaxScaleEnd, scaleLerp);
                animalScaled.ScaleDuration = Mathf.Lerp(animalParams.MinScaleDuration, animalParams.MaxScaleDuration, scaleLerp);
                animal.GetComponentInSelfOrChildren<Wander>().enabled = true;
                animal.GetComponentInSelfOrChildren<Wander>().speed = Mathf.Lerp(animalParams.MinSpeed, animalParams.MaxSpeed, scaleLerp);
                animal.GetComponentInChildren<Animation>().enabled = true;
                animal.GetComponentInChildren<Animation>()["Take 001"].speed = Mathf.Lerp(animalParams.MinAnimSpeed, animalParams.MaxAnimSpeed, scaleLerp);
            }

            foreach (var s in shards) {
                BreakShard(s);
            }
            Destroy(transform.parent.gameObject);
        }
    }
}