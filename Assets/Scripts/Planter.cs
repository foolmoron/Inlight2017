using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using Random = UnityEngine.Random;

[Serializable]
public class PlanterParams {
    [Range(0, 200)]
    public int Count = 1;
    [Range(0, 20)]
    public float SpawnRadius = 0f;
    [Range(0, 2)]
    public float SpawnInterval = 0.1f;
    [Range(0, 5)]
    public float SizeMin = 1f;
    [Range(0, 5)]
    public float SizeMax = 1f;
}

public class Planter : MonoBehaviour {

    public PlanterParams Params;
    public ImageRecord Record;

    public GameObject PlantPrefab;
    ObjectPool plantPool;
    public LayerMask CollisionMask;
    
    void Awake() {
        plantPool = PlantPrefab.GetObjectPool(1000);
    }
    
    public void DoPlanting() {
        StartCoroutine(Plant());
    }

    void OnRelease() {
        StopAllCoroutines();
    }

    readonly WaitForFixedUpdate wait = new WaitForFixedUpdate();
    IEnumerator Plant() {
        if (Params != null) {
            var i = 0;
            var timer = 0f;
            while (true) {
                if (timer <= 0) {
                    timer = Params.SpawnInterval;
                    // spawn plant
                    var randomOffset = Random.insideUnitCircle * Params.SpawnRadius;
                    var pos = new Vector3(transform.position.x + randomOffset.x, 200, transform.position.z + randomOffset.y);
                    RaycastHit hit;
                    if (Physics.Raycast(new Ray(pos, Vector3.down), out hit, 500, CollisionMask.value)) {
                        var plant = plantPool.Obtain<SpawnedObject>(hit.point);
                        plant.transform.localRotation = Quaternion.AngleAxis(Random.value * 360, Vector3.up);
                        plant.Record = Record;
                        plant.TargetScale = Mathf.Lerp(Params.SizeMin, Params.SizeMax, Random.value);
                        plant.GetComponentInChildren<Animator>().PlayFromBeginning("GrowUp");
                    }
                    i++;
                }
                timer -= Time.deltaTime;
                // exit loop if done
                if (i >= Params.Count) {
                    break;
                }
                // yield
                yield return wait;
            }
        }
        gameObject.Release();
    }
}