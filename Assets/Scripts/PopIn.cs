using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Video;

public class PopIn : MonoBehaviour {

    public AnimationCurve XCurve;
    public float AnimTime;

    RectTransform rect;

    void Start() {
        rect = GetComponent<RectTransform>();
    }
    
    void Update() {
        AnimTime += Time.deltaTime;
        rect.anchoredPosition = rect.anchoredPosition.withX(XCurve.Evaluate(AnimTime));
    }
}
