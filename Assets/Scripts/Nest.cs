using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Video;

public class Nest : MonoBehaviour {

    public GameObject EggPrefab;
    HasImageRecord record;

    [Range(0, 10)]
    public float SpawnInterval = 4f;
    [Range(0, 1)]
    public float SpawnRandomness = 0.5f;
    float spawnTime;

    [Range(0, 10)]
    public int MaxEggs = 3;
    List<GameObject> eggs = new List<GameObject>(10);

    void Start() {
        record = GetComponent<HasImageRecord>();
    }
    
    void Update() {
        spawnTime -= Time.deltaTime;
        if (spawnTime <= 0) {
            if (eggs.Count < MaxEggs) {
                var eggObj = Instantiate(EggPrefab);
                eggs.Add(eggObj);
                eggObj.transform.position = transform.position;
                eggObj.GetComponentInSelfOrChildren<HasImageRecord>().Record = record.Record;
            }
            spawnTime = SpawnInterval * (1 + Mathf.Lerp(-SpawnRandomness, SpawnRandomness, Random.value));
        }
        for (int i = 0; i < eggs.Count; i++) {
            if (eggs[i] == null) {
                eggs.RemoveAt(i);
                i--;
            }
        }
    }
}
