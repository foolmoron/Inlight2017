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

    void Start() {
        // spawn initial things when added
        ImageReader.Inst.OnAdded += record => {
            var target = SpawnViewTargets.Find(possibleTarget => possibleTarget.gameObject.activeInHierarchy);
            var forward = target.forward.withY(0).normalized; // spawn in player's view
            var dir = Quaternion.AngleAxis(Mathf.Lerp(-AngleRange, AngleRange, Random.value), Vector3.up);
            var startPos = dir * forward * Mathf.Lerp(MinDistance, MaxDistance, Random.value) + new Vector3(0, 200, 0);
            RaycastHit hit;
            Physics.Raycast(startPos, Vector3.down, out hit, 500, CollisionLayers.value);
            var spawnPos = hit.point.plusY(HeightOffsetFromGround);

            if (record.Type == ImageType.Animal) {
                // small patch of grass
                var plantRecord = ImageReader.Inst.GetWeightedRandomPlant();
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
                // nest that keeps spawning eggs
                var nestObj = Instantiate(NestPrefab);
                nestObj.transform.position = spawnPos;
                nestObj.GetComponentInSelfOrChildren<HasImageRecord>().Record = record;
            } else {
                // grass (which also spawns some seeds)
                var seedObj = Instantiate(SeedPrefab);
                seedObj.transform.position = spawnPos;
                seedObj.GetComponentInSelfOrChildren<HasImageRecord>().Record = record;
                seedObj.GetComponentInSelfOrChildren<Seed>().WillGrowPlant = true;
                seedObj.GetComponentInSelfOrChildren<Seed>().ForcedType = ImageType.Grass;
            }
        };

        // kill objects on image removed
        ImageReader.Inst.OnRemoved += record => {
            var recordObjs = FindObjectsOfType<HasImageRecord>();
            foreach (var recordObj in recordObjs) {
                if (recordObj.Record == record) {
                    Destroy(recordObj.transform.root.gameObject);
                }
            }
        };
    }

    void Update() {
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
