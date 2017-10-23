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

    public List<ImageRecord> Records = new List<ImageRecord>(100);
    public AnimationCurve RecordAgeWeighting;

    [Range(0, 5f)]
    public float FrameBudgetMillis = 1;
    readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
    ProgressiveFunc reader;

    void Start() {
        root = Application.dataPath + RootPath;
        indexPath = root + "index.txt";
        reader = new ProgressiveFunc(MainReader());
        StartCoroutine(reader);
    }
    
    void Update() {
        reader.FrameBudgetMillis = FrameBudgetMillis;
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
                var path = root + file + ".png";
                var record = Records.Find(path, (r, p) => r.Path == p);
                // create if new file in directory
                if (record == null) {
                    record = new ImageRecord {
                        Path = root + file + ".png",
                    };
                    Records.Add(record);
                    OnAdded(record);
                    yield return null;
                }
                // update file if image was changed
                if (File.GetLastWriteTime(record.Path) > record.LastUpdated) {
                    record.Texture.LoadImage(File.ReadAllBytes(record.Path));
                    record.Dimensions = new Vector2(record.Texture.width / 100f, record.Texture.height / 100f);
                    record.LastUpdated = File.GetLastWriteTime(record.Path);
                    OnUpdated(record);
                }
                yield return null;
            }
            // remove record and quad if file is deleted
            for (int i = 0; i < Records.Count; i++) {
                if (!File.Exists(Records[i].Path)) {
                    var record = Records[i];
                    Records.RemoveAt(i);
                    OnRemoved(record);
                    i--;
                }
                yield return null;
            }
            // sort by age ascending
            Records.Sort((r1, r2) => -r1.LastUpdated.CompareTo(r2.LastUpdated));
            // wait until next frame
            yield return endOfFrame;
        }
    }

    public ImageRecord GetWeightedRandomRecord() {
        var bestRecord = Records.Count > 0 ? Records[0] : null;
        var bestRecordScore = 0f;
        for (var i = 0; i < Records.Count; i++) {
            var weight = RecordAgeWeighting.Evaluate((float)i / Records.Count);
            var score = Random.value * weight;
            if (score > bestRecordScore) {
                bestRecord = Records[i];
                bestRecordScore = score;
            }
        }
        return bestRecord;
    }
}