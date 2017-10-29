using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seed : MonoBehaviour
{
    public GameObject PlantPrefab;
    ObjectPool plantPool;

    public LayerMask CollisionMask;

    ImageRecord record;
    Renderer seedRenderer;

    void Awake() {
        seedRenderer = GetComponent<Renderer>();
    }

    void Start() {
        plantPool = PlantPrefab.GetObjectPool(1000);
    }

    void Update() {
        if (record == null) {
            record = ImageReader.Inst.GetWeightedRandomRecord();
        }
        if (record != null) {
            seedRenderer.material.color = record.MainColor;
        }
    }

    void OnCollisionEnter(Collision collision) {
        if (((1 << collision.gameObject.layer) & CollisionMask.value) != 0) {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(transform.position.withY(200), Vector3.down), out hit, 500, CollisionMask.value)) {
                var plant = plantPool.Obtain<SpawnedObject>(hit.point);
                plant.TargetScale = Mathf.Lerp(0.2f, 3f, Random.value);
                plant.GetComponentInChildren<Animator>().PlayFromBeginning("GrowUp");
            }

            Destroy(gameObject);
        }
    }
}