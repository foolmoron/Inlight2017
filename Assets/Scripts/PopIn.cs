using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Video;

public class PopIn : MonoBehaviour {

    public AnimationCurve XCurve;

    RectTransform rect;

    void Start() {
        rect = GetComponent<RectTransform>();
    }
    
    void Update() {
        rect.anchoredPosition = rect.anchoredPosition.withX(XCurve.Evaluate(Time.time));
    }
}
