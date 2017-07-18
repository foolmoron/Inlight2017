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
    public string Path;
    public Texture2D Texture = new Texture2D(2, 2);
    public DateTime LastUpdated;
}

public class ImageReader : Manager<ImageReader> {

    public event Action<ImageRecord> OnAdded = delegate { };
    public event Action<ImageRecord> OnUpdated = delegate { };
    public event Action<ImageRecord> OnRemoved = delegate { };

    public string RootPath = @"\..\TestImages\";

    public GameObject QuadPrefab;

    public ListDict<string, ImageRecord> Records = new ListDict<string, ImageRecord>(100);

    [Range(0, 5f)]
    public float FrameBudgetMillis = 1;
    Stopwatch sw = new Stopwatch();
    readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

    void Start() {
        StartCoroutine(MainReader());
    }
    
    void Update() {
    }

    IEnumerator MainReader() {
        while (true) {
            // timing stuff
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
                    var newQuad = Instantiate(QuadPrefab, Vector3.down * 1000, Quaternion.identity);
                    Records[file] = new ImageRecord {
                        Quad = newQuad.GetComponent<MeshRenderer>(),
                        Path = file,
                    };
                    OnAdded(Records[file]);
                    /* FRAME BREAK */
                    if (sw.ElapsedTicks >= tickBudget) { yield return endOfFrame; sw.Restart(); }
                }
                // update file if image was changed
                var record = Records[file];
                var lastWrite = File.GetLastWriteTime(record.Path);
                if (lastWrite > record.LastUpdated) {
                    record.Texture.LoadImage(File.ReadAllBytes(record.Path));
                    record.Quad.material.mainTexture = record.Texture;
                    record.LastUpdated = lastWrite;
                    OnUpdated(record);
                }
                /* FRAME BREAK */ if (sw.ElapsedTicks >= tickBudget) { yield return endOfFrame; sw.Restart(); }
            }
            // remove record and quad if file is deleted
            for (int i = 0; i < Records.Count; i++) {
                if (!File.Exists(Records.Values[i].Path)) {
                    var record = Records.Values[i];
                    Destroy(Records.Values[i].Quad.gameObject);
                    Records.RemoveAt(i);
                    OnRemoved(record);
                    i--;
                }
                /* FRAME BREAK */ if (sw.ElapsedTicks >= tickBudget) { yield return endOfFrame; sw.Restart(); }
            }
            // wait until next frame
            yield return endOfFrame;
        }
    }
}