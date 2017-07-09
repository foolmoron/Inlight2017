using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class ImageRecord {
    public MeshRenderer Quad;
    public MeshRenderer[] Quads;
    public string Path;
    public Texture2D Texture = new Texture2D(2, 2);
    public DateTime LastUpdated;
}

public class ImageReader : MonoBehaviour {

    public string RootPath = @"\..\TestImages\";

    public GameObject QuadPrefab;

    public ListDict<string, ImageRecord> Records = new ListDict<string, ImageRecord>(100);

    [Range(0, 5f)]
    public float FrameBudgetMillis = 1;
    Stopwatch sw = new Stopwatch();
    int framesSinceLastFilePass;
    readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

    void Start() {
        StartCoroutine(MainReader());
    }
    
    void Update() {
        framesSinceLastFilePass++;
    }

    IEnumerator MainReader() {
        while (true) {
            // timing stuff
            //Debug.Log("Frames: " + framesSinceLastFilePass + " Ticks: " + sw.ElapsedTicks);
            framesSinceLastFilePass = 0;
            var tickBudget = (int) (FrameBudgetMillis * 10000);
            sw.Restart();
            // get files
            var root = Application.dataPath + RootPath;
            var files = Directory.GetFiles(root);
            /* FRAME BREAK */ if (sw.ElapsedTicks >= tickBudget) { yield return endOfFrame; sw.Restart(); }
            // loop files
            foreach (var file in files) {
                // create if new file in directory
                if (!Records.ContainsKey(file)) {
                    var newQuad = Instantiate(QuadPrefab, new Vector3((Random.value - 0.5f) * 2, Random.value * 3 + 1, (Random.value - 0.5f) * 3), Quaternion.Euler(Random.onUnitSphere * 45));
                    Records[file] = new ImageRecord {
                        Quad = newQuad.GetComponent<MeshRenderer>(),
                        Quads = newQuad.GetComponentsInChildren<MeshRenderer>(),
                        Path = file,
                    };
                    /* FRAME BREAK */ if (sw.ElapsedTicks >= tickBudget) { yield return endOfFrame; sw.Restart(); }
                }
                // update file if image was changed
                var record = Records[file];
                var lastWrite = File.GetLastWriteTime(record.Path);
                if (lastWrite > record.LastUpdated) {
                    record.Texture.LoadImage(File.ReadAllBytes(record.Path));
                    record.Quad.material.mainTexture = record.Texture;
                    foreach (var quad in record.Quads) {
                        quad.material.mainTexture = record.Texture;
                    }
                    record.LastUpdated = lastWrite;
                }
                /* FRAME BREAK */ if (sw.ElapsedTicks >= tickBudget) { yield return endOfFrame; sw.Restart(); }
            }
            // remove record and quad if file is deleted
            for (int i = 0; i < Records.Count; i++) {
                if (!File.Exists(Records.Values[i].Path)) {
                    Destroy(Records.Values[i].Quad.gameObject);
                    Records.RemoveAt(i);
                    i--;
                }
                /* FRAME BREAK */ if (sw.ElapsedTicks >= tickBudget) { yield return endOfFrame; sw.Restart(); }
            }
            // wait until next frame
            yield return endOfFrame;
        }
    }
}