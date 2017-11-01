using UnityEngine;
using System.Collections;

public class DieBelowY : MonoBehaviour {

    public float Y;
    public bool Release;
    
    void Update() {
        if (transform.position.y < 0) {
            if (Release) {
                gameObject.Release();
            } else {
                Destroy(gameObject);
            }
        }
    }
}
