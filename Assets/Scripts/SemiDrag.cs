using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Video;

public class SemiDrag : MonoBehaviour {

    public float MinDrag = 0f;
    public float MaxDrag = 10000f;
    public float TouchForce = 50;

    public AnimationCurve DragCurve;
    public float DragTime;
    public bool ModdingDrag = true;

    Rigidbody rb;

    void Start() {
        rb = GetComponent<Rigidbody>();
    }

    void Update() {
        if (ModdingDrag) {
            DragTime += Time.deltaTime;
            var lerp = DragCurve.Evaluate(DragTime);
            if (lerp >= 1) {
                rb.drag = MaxDrag;
                ModdingDrag = false;
            } else {
                rb.drag = Mathf.Lerp(MinDrag, MaxDrag, lerp);
            }
        }
    }

    public void GetTouched(Vector3 position, Vector3 direction) {
        ModdingDrag = true;
        DragTime = 0;
        rb.drag = MinDrag;
        rb.AddForceAtPosition(direction * TouchForce, position);
    }
}
