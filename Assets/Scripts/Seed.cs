using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using Random = UnityEngine.Random;

public class Seed : MonoBehaviour
{
    public GameObject PlanterPrefab;
    ObjectPool planterPool;

    public LayerMask CollisionMask;
    public bool WillGrowPlant;

    [Range(0, 300)]
    public float DeathTime = 180;
    float deathTime;

    public ImageRecord Record;
    Renderer seedRenderer;
    new Collider collider;

    public PlanterParams TreeParams;
    public PlanterParams BushParams;
    public PlanterParams GrassParams;

    void Awake() {
        seedRenderer = GetComponent<Renderer>();
        collider = GetComponent<Collider>();

        var interactable = GetComponent<VRTK_InteractableObject>();
        interactable.InteractableObjectGrabbed += (sender, args) => {
            collider.enabled = false;
        };
        GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed += (sender, args) => {
            collider.enabled = true;
            WillGrowPlant = true;
        };
    }
    
    void OnObtain() {
        collider.enabled = true;
        WillGrowPlant = false;
    }

    void Start() {
        planterPool = PlanterPrefab.GetObjectPool(100);
    }

    void Update() {
        if (Record == null) {
            Record = ImageReader.Inst.GetWeightedRandomPlant();
        }
        if (Record != null) {
            seedRenderer.material.color = Record.MainColor;
        }
        deathTime += Time.deltaTime;
        if (deathTime >= DeathTime) {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision) {
        if (!WillGrowPlant) {
            return;
        }
        if (((1 << collision.gameObject.layer) & CollisionMask.value) != 0) {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(transform.position.withY(200), Vector3.down), out hit, 500, CollisionMask.value)) {
                var planter = planterPool.Obtain<Planter>(hit.point);
                planter.Record = Record;
                switch (Record.Type) {
                    case ImageType.Tree:
                        planter.Params = TreeParams;
                        break;
                    case ImageType.Bush:
                        planter.Params = BushParams;
                        break;
                    case ImageType.Grass:
                        planter.Params = GrassParams;
                        break;
                    default:
                        var r = Random.value;
                        planter.Params = r <= 0.33f ? TreeParams : r <= 0.66f ? BushParams : GrassParams;
                        break;
                }
                planter.DoPlanting();
            }

            Destroy(gameObject);
        }
    }
}