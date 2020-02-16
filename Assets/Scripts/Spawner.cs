using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour {

    public GameObject SeedPrefab;
    public GameObject EggPrefab;
    public GameObject NestPrefab;

    public Transform[] SpawnViewTargets;
    public float MinDistance;
    public float MaxDistance;
    [Range(0, 360)]
    public float AngleRange = 100;

    public LayerMask CollisionLayers;
    public float HeightOffsetFromGround;

    [Range(0, 40)]
    public int MaxInitialRecords = 10;
    [Range(0, 15)]
    public float InitialRecordsPeriod = 2;
    float initialRecordsTime;
    [Range(0, 5)]
    public float AutoSpawnInterval = 1;
    float autoSpawnTime;
    readonly Queue<ImageRecord> autoSpawnQueue = new Queue<ImageRecord>(10);
    readonly Queue<ImageRecord> initialRecords = new Queue<ImageRecord>(10);

    void Start() {
        // spawn initial things when added
        ImageReader.Inst.OnAdded += record => {
            if (initialRecordsTime < InitialRecordsPeriod && initialRecords.Count < MaxInitialRecords) {
                initialRecords.Enqueue(record);
            } else {
                autoSpawnQueue.Enqueue(record);
            }
        };

        // kill objects on image removed
        ImageReader.Inst.OnRemoved += record => {
            foreach (var recordObj in HasImageRecord.AllInCurrentScene) {
                if (recordObj.Record == record) {
                    Destroy(recordObj.transform.root.gameObject);
                }
            }
        };
    }

    void Update() {
        if (initialRecordsTime < InitialRecordsPeriod) {
            initialRecordsTime += Time.deltaTime;
        } else {
            autoSpawnTime -= Time.deltaTime;
            if (autoSpawnTime <= 0) {
                autoSpawnTime = AutoSpawnInterval;
                // pick from next autospawn or if none are available, from initial records
                var record = autoSpawnQueue.Count > 0 ? autoSpawnQueue.Dequeue() : initialRecords.Count > 0 ? initialRecords.Dequeue() : null;
                if (record != null) {
                    // spawn
                    var startPos = transform.position + Random.insideUnitCircle.to3() * Mathf.Lerp(MinDistance, MaxDistance, Random.value);
                    var target = SpawnViewTargets.Find(possibleTarget => possibleTarget.gameObject.activeInHierarchy);
                    if (target) {
                        var forward = target.forward.withY(0).normalized; // spawn in player's view
                        var dir = Quaternion.AngleAxis(Mathf.Lerp(-AngleRange, AngleRange, Random.value), Vector3.up);
                        startPos = dir * forward * Mathf.Lerp(MinDistance, MaxDistance, Random.value);
                    }
                    RaycastHit hit;
                    Physics.Raycast(startPos + new Vector3(0, 200, 0), Vector3.down, out hit, 500, CollisionLayers.value);
                    var spawnPos = hit.point.plusY(HeightOffsetFromGround);

                    if (record.Type == ImageType.Animal) {
                        // small patch of grass
                        var plantRecord = ImageReader.Inst.GetWeightedRandomRecord(ImageType.Plant);
                        var seedObj = Instantiate(SeedPrefab);
                        seedObj.transform.position = spawnPos;
                        seedObj.GetComponentInSelfOrChildren<HasImageRecord>().Record = plantRecord;
                        seedObj.GetComponentInSelfOrChildren<Seed>().WillGrowPlant = true;
                        seedObj.GetComponentInSelfOrChildren<Seed>().ForcedType = ImageType.TinyPatch;
                        // egg
                        var eggObj = Instantiate(EggPrefab);
                        eggObj.transform.position = spawnPos;
                        eggObj.GetComponentInSelfOrChildren<HasImageRecord>().Record = record;
                        eggObj.GetComponentInSelfOrChildren<Egg>().AutoBreak = true;
                        eggObj.GetComponentInSelfOrChildren<Egg>().ForceGroupParams = true;
                        // nest that keeps spawning eggs
                        var nestObj = Instantiate(NestPrefab);
                        nestObj.transform.position = spawnPos;
                        nestObj.GetComponentInSelfOrChildren<HasImageRecord>().Record = record;
                    } else {
                        // any type of plant
                        var seedObj = Instantiate(SeedPrefab);
                        seedObj.transform.position = spawnPos;
                        seedObj.GetComponentInSelfOrChildren<HasImageRecord>().Record = record;
                        seedObj.GetComponentInSelfOrChildren<Seed>().WillGrowPlant = true;
                        //seedObj.GetComponentInSelfOrChildren<Seed>().ForcedType = ImageType.Grass;
                    }
                }
            }
        }
    }

    void OnDrawGizmosSelected() {
        if (!SpawnViewTargets.Find(possibleTarget => possibleTarget.gameObject.activeInHierarchy)) {
            Gizmos.color = Color.red.withAlpha(0.25f);
            Gizmos.DrawSphere(transform.position, MinDistance);
            Gizmos.color = Color.white.withAlpha(0.25f);
            Gizmos.DrawSphere(transform.position, MaxDistance);
        }
    }

    //void Spawn(ImageRecord record) {
    //    var obj = Instantiate(record.Type == ImageType.Animal ? EggPrefab : SeedPrefab);

    //    var egg = obj.GetComponentInSelfOrChildren<Egg>();
    //    if (egg) {
    //        eggs.Add(egg);
    //        egg.Record = record;
    //    }
    //    var seed = obj.GetComponentInSelfOrChildren<Seed>();
    //    if (seed) {
    //        seeds.Add(seed);
    //        seed.Record = record;
    //    }

    //    var dir = Random.insideUnitCircle.normalized;
    //    var randomStart = dir.scaledWith(new Vector2(PositionRandomness.x, PositionRandomness.z));
    //    var randomOffset = new Vector3(randomStart.x, 0, randomStart.y) + new Vector3(dir.x, 0, dir.y).scaledWith(new Vector3(Random.value * PositionRandomness.x, Random.value * PositionRandomness.y, Random.value * PositionRandomness.z));
    //    var startPosition =
    //        TargetTransform.position +
    //        randomOffset +
    //        new Vector3(0, 200, 0)
    //        ;
    //    RaycastHit hit;
    //    Physics.Raycast(startPosition, Vector3.down, out hit, 500, CollisionLayers.value);
    //    obj.transform.position = hit.point.plusY(HeightOffsetFromGround);
    //}


}
