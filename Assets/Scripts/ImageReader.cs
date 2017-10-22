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
    public Vector2 Dimensions;
    public DateTime LastUpdated;
}

public class ImageReader : Manager<ImageReader> {

    public event Action<ImageRecord> OnAdded = delegate { };
    public event Action<ImageRecord> OnUpdated = delegate { };
    public event Action<ImageRecord> OnRemoved = delegate { };

    public string RootPath = @"\..\Images\";

    string root;
    string indexPath;
    DateTime lastIndexWrite;
    List<string> files = new List<string>(1000);

    public GameObject QuadPrefab;

    public ListDict<string, ImageRecord> Records = new ListDict<string, ImageRecord>(100);

    [Range(0, 5f)]
    public float FrameBudgetMillis = 1;
    Stopwatch sw = new Stopwatch();
    readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

    void Start() {
        root = Application.dataPath + RootPath;
        indexPath = root + "index.txt";
        StartCoroutine(new ProgressiveFunc(MainReader()));
    }
    
    void Update() {
    }

    IEnumerator MainReader() {
        while (true) {
            // get file index if changed
            if (File.GetLastWriteTime(indexPath) > lastIndexWrite) {
                lastIndexWrite = File.GetLastWriteTime(indexPath);
                files.Clear();
                using (var fileReader = File.OpenText(indexPath)) {
                    while (!fileReader.EndOfStream) {
                        files.Add(fileReader.ReadLine());
                    }
                }
            }
            yield return null;
            // loop files
            foreach (var file in files) {
                // create if new file in directory
                if (!Records.ContainsKey(file)) {
                    var newQuad = Instantiate(QuadPrefab, Vector3.down * 1000, Quaternion.identity);
                    Records[file] = new ImageRecord {
                        Quad = newQuad.GetComponent<MeshRenderer>(),
                        Path = root + file + ".png",
                    };
                    OnAdded(Records[file]);
                    yield return null;
                }
                // update file if image was changed
                var record = Records[file];
                if (File.GetLastWriteTime(record.Path) > record.LastUpdated) {
                    record.Texture.LoadImage(File.ReadAllBytes(record.Path));
                    record.Quad.material.mainTexture = record.Texture;
                    record.Dimensions = new Vector2(record.Texture.width / 100f, record.Texture.height / 100f);
                    record.LastUpdated = File.GetLastWriteTime(record.Path);
                    OnUpdated(record);
                }
                yield return null;
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
                yield return null;
            }
            // wait until next frame
            yield return endOfFrame;
        }
    }
}