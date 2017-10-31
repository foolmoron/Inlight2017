using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class Seed : MonoBehaviour
{
    public GameObject PlantPrefab;
    ObjectPool plantPool;

    public LayerMask CollisionMask;
    public bool WillGrowPlant;

    public ImageRecord Record;
    Renderer seedRenderer;

    void Awake() {
        seedRenderer = GetComponent<Renderer>();
        GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed += (sender, args) => WillGrowPlant = true;
    }

    void Start() {
        plantPool = PlantPrefab.GetObjectPool(1000);
    }

    void Update() {
        if (Record == null) {
            Record = ImageReader.Inst.GetWeightedRandomRecord();
        }
        if (Record != null) {
            seedRenderer.material.color = Record.MainColor;
        }
    }

    void OnCollisionEnter(Collision collision) {
        if (!WillGrowPlant) {
            return;
        }
        if (((1 << collision.gameObject.layer) & CollisionMask.value) != 0) {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(transform.position.withY(200), Vector3.down), out hit, 500, CollisionMask.value)) {
                var plant = plantPool.Obtain<SpawnedObject>(hit.point);
                plant.Record = Record;
                plant.TargetScale = Mathf.Lerp(0.2f, 3f, Random.value);
                plant.GetComponentInChildren<Animator>().PlayFromBeginning("GrowUp");
            }

            Destroy(gameObject);
        }
    }
}