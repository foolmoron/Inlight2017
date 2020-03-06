using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Video;

public class Pauser : MonoBehaviour {

    public KeyCode Key = KeyCode.Space;
    [Range(0.001f, 5)]
    public float TargetTimescale = 1;
    [Range(0, 0.5f)]
    public float TimescaleSpeed = 0.25f;

    void Start() {
        StartCoroutine(PauseLoop());
    }
    
    IEnumerator PauseLoop() {
        while (true) {
            Time.timeScale = Mathf.Lerp(Time.timeScale, TargetTimescale, TimescaleSpeed);
            if (Input.GetKeyDown(Key)) {
                TargetTimescale = TargetTimescale > 0.5f ? 0.001f : 1f;
            }
            yield return null;
        }
    }
}
