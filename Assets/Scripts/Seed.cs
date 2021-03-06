﻿using System;
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

    HasImageRecord record;
    Renderer seedRenderer;
    ParticleSystem particles;
    new Collider collider;

    [Range(0, 1)]
    public float TreePerc = 0.33f;
    [Range(0, 1)]
    public float BushPerc = 0.33f;
    [Range(0, 1)]
    public float GrassPerc = 0.34f;

    public PlanterParams TreeParams;
    public PlanterParams BushParams;
    public PlanterParams GrassParams;
    public PlanterParams TinyPatchParams;

    public ImageType? ForcedType;

    void Awake() {
        seedRenderer = GetComponent<Renderer>();
        collider = GetComponent<Collider>();
        record = GetComponent<HasImageRecord>();
        particles = GetComponentInChildren<ParticleSystem>();

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

        seedRenderer.material.EnableKeyword("_EMISSION");
    }

    void Update() {
        if (record.Record == null) {
            record.Record = ImageReader.Inst.GetWeightedRandomRecord(ImageType.Plant);
        }
        if (record.Record != null) {
            seedRenderer.material.color = record.Record.MainColor;
            seedRenderer.material.SetColor("_EmissionColor", record.Record.MainColor);
            particles.setColor(record.Record.MainColor);
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
                planter.GetComponent<HasImageRecord>().Record = record.Record;
                switch (ForcedType ?? record.Record.Type) {
                    case ImageType.Tree:
                        planter.Params = TreeParams;
                        break;
                    case ImageType.Bush:
                        planter.Params = BushParams;
                        break;
                    case ImageType.Grass:
                        planter.Params = GrassParams;
                        break;
                    case ImageType.TinyPatch:
                        planter.Params = TinyPatchParams;
                        break;
                    default:
                        var r = Random.value;
                        planter.Params = r <= TreePerc ? TreeParams : r <= (TreePerc + BushPerc) ? BushParams : r <= (TreePerc + BushPerc + GrassPerc) ? GrassParams : TinyPatchParams;
                        break;
                }
                planter.DoPlanting();
            }

            Destroy(gameObject);
        }
    }
}