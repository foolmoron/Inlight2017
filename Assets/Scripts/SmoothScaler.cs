using UnityEngine;
using System.Collections;

public class SmoothScaler : MonoBehaviour {

    public float OriginalScale;
    public float TargetScale;
    public float ScaleDuration;
    float scaleTime;

    SpawnedObject obj;

    void Awake() {
        obj = GetComponent<SpawnedObject>();
    }

    void OnObtain() {
        scaleTime = 0;
    }

    void FixedUpdate() {
        scaleTime += Time.deltaTime;
        var lerp = Mathf.Clamp01(scaleTime / ScaleDuration);
        var scale = Mathf.Lerp(OriginalScale, TargetScale, lerp);
        obj.ScaleFactor = scale;
    }
}
