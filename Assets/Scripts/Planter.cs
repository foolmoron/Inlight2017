using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using Random = UnityEngine.Random;

[Serializable]
public class PlanterParams {
    [Range(0, 500)]
    public int CountMin = 1;
    [Range(0, 500)]
    public int CountMax = 1;
    [Range(0, 20)]
    public float SpawnRadius = 0f;
    [Range(0, 0.25f)]
    public float SpawnInterval = 0.1f;
    [Range(0, 10)]
    public float SizeMin = 1f;
    [Range(0, 10)]
    public float SizeMax = 1f;
    [Range(0, 5)]
    public float MinAnimSpeed;
    [Range(0, 5)]
    public float MaxAnimSpeed;
    [Range(1, 10)]
    public int ForkCount = 1;
    [Range(0, 1)]
    public float ForkSpeed;
    [Range(0, 30)]
    public float ForkRotation;
}

public class Planter : MonoBehaviour {

    public PlanterParams Params;
    HasImageRecord record;

    public GameObject PlantPrefab;
    ObjectPool plantPool;
    public LayerMask CollisionMask;
    List<Vector2> forks = new List<Vector2>(10);
    List<Vector2> forkDirections = new List<Vector2>(10);
    List<float> forkRotations = new List<float>(10);

    void Awake() {
        plantPool = PlantPrefab.GetObjectPool(1000);
        record = GetComponent<HasImageRecord>();
    }
    
    public void DoPlanting() {
        StartCoroutine(Plant());
    }

    void OnRelease() {
        StopAllCoroutines();
    }
    
    IEnumerator Plant() {
        if (Params != null) {
            forks.Clear();
            forkDirections.Clear();
            forkRotations.Clear();
            for (int x = 0; x < Params.ForkCount; x++) {
                forks.Add(Vector3.zero);
                forkDirections.Add(Random.insideUnitCircle.normalized);
                forkRotations.Add(Mathf.Lerp(-Params.ForkRotation, Params.ForkRotation, Random.value));
            }

            var i = 0;
            var timer = 0f;
            var plantCount = Random.Range(Params.CountMin, Params.CountMax);
            while (true) {
                // spawn at interval
                if (timer <= 0) {
                    timer += Params.SpawnInterval;
                    var forkIndex = Mathf.FloorToInt(Random.value * forks.Count);
                    var fork = forks[forkIndex];
                    // move fork a bit after it is picked
                    forks[forkIndex] += Params.ForkSpeed * forkDirections[forkIndex];
                    // rotate fork direction as well
                    forkDirections[forkIndex] = forkDirections[forkIndex].Rotate(forkRotations[forkIndex]);
                    // spawn plant
                    var randomOffset = Random.insideUnitCircle * Params.SpawnRadius;
                    var pos = (transform.position + new Vector3(fork.x, 0, fork.y) + new Vector3(randomOffset.x, 0, randomOffset.y)).withY(200);
                    RaycastHit hit;
                    if (Physics.Raycast(new Ray(pos, Vector3.down), out hit, 500, CollisionMask.value)) {
                        var plant = plantPool.Obtain<SpawnedObject>(hit.point);
                        plant.transform.localRotation = Quaternion.AngleAxis(Random.value * 360, Vector3.up);
                        plant.GetComponent<HasImageRecord>().Record = record.Record;
                        var scaleLerp = Random.value;
                        plant.TargetScale = Mathf.Lerp(Params.SizeMin, Params.SizeMax, scaleLerp);
                        plant.Properties.SetFloat("_Timescale", Mathf.Lerp(Params.MaxAnimSpeed, Params.MinAnimSpeed, scaleLerp));
                        plant.Properties.SetFloat("_TimeOffset", Random.value * 5);
                    }
                    i++;
                }
                timer -= Time.deltaTime;
                // exit loop if done
                if (i >= plantCount) {
                    break;
                }
                // yield
                yield return null;
            }
        }
        gameObject.Release();
    }
}