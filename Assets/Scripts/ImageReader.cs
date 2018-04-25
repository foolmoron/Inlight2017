using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public enum ImageType {
    Animal, Plant, Tree, Bush, Grass, TinyPatch
}
public enum ImageFacing {
    Left, Right
}

[Serializable]
public class ImageRecord {
    public string Name;
    public string Path;
    public ImageType Type;
    public ImageFacing Facing;
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

    public static Dictionary<Texture2D, Material> MaterialsCache = new Dictionary<Texture2D, Material>();

    string root;
    string indexPath;
    DateTime lastIndexWrite;
    List<string> files = new List<string>(1000);
    List<ImageType> types = new List<ImageType>(1000);
    List<ImageFacing> facings = new List<ImageFacing>(1000);

    readonly Dictionary<char, ImageType> CHAR_TO_TYPE = new Dictionary<char, ImageType> {
        {'a', ImageType.Animal },
        {'p', ImageType.Plant },
        {'t', ImageType.Tree },
        {'b', ImageType.Bush },
        {'g', ImageType.Grass },
    };
    readonly Dictionary<char, ImageFacing> CHAR_TO_FACING = new Dictionary<char, ImageFacing> {
        {'l', ImageFacing.Left },
        {'r', ImageFacing.Right },
    };

    public List<ImageRecord> Records = new List<ImageRecord>(100);
    public AnimationCurve RecordAgeWeighting;

    [Range(0, 5f)]
    public float FrameBudgetMillis = 1;
    readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
    ProgressiveFunc reader;

    List<ImageRecord> recordsJustAdded = new List<ImageRecord>(20);

    IEnumerator Start() {
        root = Application.dataPath + RootPath;
        indexPath = root + "index.txt";
        reader = new ProgressiveFunc(MainReader());
        yield return new WaitForSeconds(1f);
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
                types.Clear();
                facings.Clear();
                using (var fileReader = File.OpenText(indexPath)) {
                    while (!fileReader.EndOfStream) {
                        var line = fileReader.ReadLine() ?? "";
                        files.Add(line.Substring(0, 36)); // 36 char uuid
                        types.Add(CHAR_TO_TYPE[line[line.Length - 3]]); // type
                        facings.Add(CHAR_TO_FACING[line[line.Length - 1]]); // facing
                    }
                }
            }
            yield return null;
            // loop files
            for (var i = 0; i < files.Count; i++) {
                var file = files[i];
                var type = types[i];
                var facing = facings[i];
                var path = root + file + ".png";
                var record = Records.Find(path, (r, p) => r.Path == p);
                // create if new file in directory
                if (record == null) {
                    record = new ImageRecord {
                        Name = file,
                        Path = root + file + ".png",
                        Type = type,
                        Facing = facing,
                        Texture = new Texture2D(2, 2),
                    };
                    record.Texture.wrapMode = TextureWrapMode.Clamp; // eliminate slight artifacts at edges of image
                    Records.Add(record);
                    recordsJustAdded.Add(record);
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
            yield return null;
            // call added events
            foreach (var record in recordsJustAdded) {
                OnAdded(record);
            }
            recordsJustAdded.Clear();
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

    public ImageRecord GetWeightedRandomRecord(bool animal, bool plant) {
        var bestRecord = Records.Count > 0 ? Records[0] : null;
        var bestRecordScore = 0f;
        for (var i = 0; i < Records.Count; i++) {
            var weight = RecordAgeWeighting.Evaluate((float)i / Records.Count);
            var score = Random.value * weight;
            if (score > bestRecordScore && ((animal && Records[i].Type == ImageType.Animal) || (plant && Records[i].Type != ImageType.Animal))) {
                bestRecord = Records[i];
                bestRecordScore = score;
            }
        }
        return bestRecord;
    }

    public ImageRecord GetWeightedRandomAnimal() {
        return GetWeightedRandomRecord(true, false);
    }
    public ImageRecord GetWeightedRandomPlant() {
        return GetWeightedRandomRecord(false, true);
    }
}