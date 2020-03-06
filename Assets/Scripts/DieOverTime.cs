using UnityEngine;
using System.Collections;

public class DieOverTime : MonoBehaviour {

    public AnimationCurve DieCurve;
    public float LifeTime;
    Vector3 originalScale;

    void Start() {
        originalScale = transform.localScale;
    }

    void Update() {
        LifeTime += Time.deltaTime;
        transform.localScale = originalScale * DieCurve.Evaluate(LifeTime);
        if (DieCurve.Evaluate(LifeTime) <= 0) {
            if (gameObject.GetComponent<PooledObject>())
                gameObject.Release();
            else
                Destroy(gameObject);
        }
    }
}
