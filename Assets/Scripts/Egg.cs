using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Egg : MonoBehaviour
{
    public GameObject AnimalPrefab;
    ObjectPool animalPool;

    [Range(0, 5)]
    public int Health = 3;

    [Range(0, 10)]
    public float AnimalOriginalScale = 0.2f;
    [Range(0, 10)]
    public float AnimalFinalScale = 2f;
    [Range(0, 1)]
    public float AnimalScaleSpeed = 0.2f;

    public ImageRecord Record;
    GameObject solid;
    List<GameObject> shards;

    SpawnedObject animal;

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
        animal = animalPool.Obtain<SpawnedObject>();
        animal.ScaleFactor = animal.TargetScale = AnimalOriginalScale;
        animal.transform.parent = transform.parent;
        animal.transform.localPosition = Vector3.zero;
        animal.GetComponentInSelfOrChildren<Wander>().enabled = false;
        animal.GetComponentInChildren<Animation>().enabled = false;
    }

    void Update() {
        if (Record == null) {
            Record = ImageReader.Inst.GetWeightedRandomRecord();
        }
        if (Record != null) {
            animal.Record = Record;
            solid.GetComponent<Renderer>().material.color = Record.MainColor;
            foreach (var shard in shards) {
                shard.GetComponent<Renderer>().material.color = Record.MainColor;
            }
        }
        // animal stays up right
        {
            animal.transform.rotation = Quaternion.identity;
        }
    }

    void BreakShard(GameObject shard) {
        shard.transform.parent = null;
        shard.GetComponent<Collider>().enabled = true;
        shard.GetComponent<Rigidbody>().isKinematic = false;
        shard.GetComponent<Animator>().enabled = false;
    }
    
    void OnTriggerEnter(Collider other) {

        Debug.Log(other.name);

        solid.SetActive(false);

        var i = 3 - Health;
        if (shards.Count > i) {
            shards[i].GetComponent<Animator>().enabled = true;
        }

         Health--;
        if (Health <= 0) {
            animal.TargetScale = AnimalFinalScale;
            animal.ScaleSpeed = AnimalScaleSpeed;
            animal.transform.parent = null;
            animal.GetComponentInSelfOrChildren<Wander>().enabled = true;
            animal.GetComponentInChildren<Animation>().enabled = true;

            foreach (var s in shards) {
                BreakShard(s);
            }
            Destroy(gameObject);
        }
    }
}