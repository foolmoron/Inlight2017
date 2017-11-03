using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour {

    public GameObject SeedPrefab;
    ObjectPool seedPool;

    public GameObject EggPrefab;
    ObjectPool eggPool;

    List<Egg> eggs = new List<Egg>(100);
    List<Seed> seeds = new List<Seed>(100);

    public LayerMask CollisionLayers;
    public float HeightOffsetFromGround;

    public Transform TargetTransform;
    public Vector3 PositionRandomness;

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
            for (int i = 0; i < eggs.Count; i++) {
                if (eggs[i].Record == record) {
                    Destroy(eggs[i].gameObject);
                    eggs.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < seeds.Count; i++) {
                if (seeds[i].Record == record) {
                    seeds[i].gameObject.Release();
                    seeds.RemoveAt(i);
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
                var record = ImageReader.Inst.GetWeightedRandomRecord();
                if (record != null) {
                    Spawn(record);
                    SpawnTimer = Random.value * SpawnIntervalRandomness;
                }
            }
        }
    }
    
    void Spawn(ImageRecord record) {
        var obj = (record.Type == ImageType.Animal ? eggPool : seedPool).Obtain();

        var egg = obj.GetComponentInSelfOrChildren<Egg>();
        if (egg) {
            eggs.Add(egg);
            egg.Record = record;
        }
        var seed = obj.GetComponentInSelfOrChildren<Seed>();
        if (seed) {
            seeds.Add(seed);
            seed.Record = record;
        }

        var dir = Random.insideUnitCircle.normalized;
        var randomStart = dir.scaledWith(new Vector2(PositionRandomness.x, PositionRandomness.z));
        var randomOffset = new Vector3(randomStart.x, 0, randomStart.y) + new Vector3(dir.x, 0, dir.y).scaledWith(new Vector3(Random.value * PositionRandomness.x, Random.value * PositionRandomness.y, Random.value * PositionRandomness.z));
        var startPosition =
            TargetTransform.position +
            randomOffset +
            new Vector3(0, 200, 0)
            ;
        RaycastHit hit;
        Physics.Raycast(startPosition, Vector3.down, out hit, 500, CollisionLayers.value);
        obj.transform.position = hit.point.plusY(HeightOffsetFromGround);
    }
}
