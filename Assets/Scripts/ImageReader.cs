﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

[Serializable]
public class ImageRecord {
    public string Name;
    public string Path;
    public Vector2 Dimensions;
    public DateTime LastUpdated;
    public Color MainColor;

    public const int MAIN_COLOR_MIP_LEVEL = 3;
    public Texture2D Texture { get; set; }
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
        var x = new Stopwatch();
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
                        Name = file,
                        Path = root + file + ".png",
                        Texture = new Texture2D(2, 2),
                    };
                    Records.Add(record);
                    OnAdded(record);
                    yield return null;
                }
                // update file if image was changed
                if (File.Exists(record.Path) && File.GetLastWriteTime(record.Path) > record.LastUpdated) {
                    // do all file-reading work in one atomic chunk
                    record.LastUpdated = File.GetLastWriteTime(record.Path);
                    record.Texture.LoadImage(File.ReadAllBytes(record.Path));
                    yield return endOfFrame;
                    var pixels = record.Texture.GetPixels32(ImageRecord.MAIN_COLOR_MIP_LEVEL);
                    yield return null;
                    record.MainColor = GetMainColorFromPixels(pixels, 2); // blend top 2 colors
                    yield return null;
                    record.Dimensions = new Vector2(record.Texture.width / 100f, record.Texture.height / 100f);
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

    static Color GetMainColorFromPixels(Color32[] pixels, int blendTopColors = 1) {
        // get frequency of each solid pixel color
        var pixelFrequencies = new Dictionary<Color32, int>();
        foreach (var pixel in pixels) {
            if (pixel.a > 128) {
                var freq = pixelFrequencies.ContainsKey(pixel) ? pixelFrequencies[pixel] : 0;
                pixelFrequencies[pixel] = freq + 1;
            }
        }
        // black if nothing
        if (pixelFrequencies.Count == 0) {
            return Color.black;
        }
        // get most common colors
        var topColors = pixelFrequencies.OrderByDescending(pair => pair.Value).Select(pair => (Color)pair.Key).ToList();
        // blend top colors together in equal proportion
        var finalColor = topColors[0];
        for (int i = 1; i < topColors.Count && i < blendTopColors; i++) {
            finalColor = Color.Lerp(finalColor, topColors[i], 1f / (i + 1));
        }
        // max sat/val of final color
        finalColor = Color.HSVToRGB(HSBColor.FromColor(finalColor).h, 1, 1);
        return finalColor;
    }

    static Color GetAverageColorFromPixels(Color[] pixels) {
        // blank pixels to start
        var r = 0f;
        var g = 0f;
        var b = 0f;
        // each pixel adds to rgb based on the ratio of pixel alpha to total alpha of all pixels
        var totalAlpha = pixels.Sum(c => c.a);
        foreach (var pixel in pixels) {
            var weight = pixel.a / totalAlpha;
            r += pixel.r * weight;
            g += pixel.g * weight;
            b += pixel.b * weight;
        }
        // max sat/bright of color
        var hsbColor = HSBColor.FromColor(new Color(r, g, b));
        hsbColor.s = 1;
        hsbColor.b = 1;
        return hsbColor.ToColor();
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