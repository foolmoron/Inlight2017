using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;

public class HasImageRecord : MonoBehaviour {

    public static List<HasImageRecord> AllInCurrentScene = new List<HasImageRecord>();

    public ImageRecord Record;

    void Awake() {
        AllInCurrentScene.Add(this);
    }

    void OnDestroy() {
        AllInCurrentScene.Remove(this);
    }
}
