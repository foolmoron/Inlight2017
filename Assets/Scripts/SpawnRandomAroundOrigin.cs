using UnityEngine;
using System.Collections;

public class SpawnRandomAroundOrigin : MonoBehaviour {

    public GameObject QuadPrefab;

    public Vector3 PositionOffset;
    public Vector3 PositionRandomness;
    public Vector3 RotationRandomness;
    
    void Start() {
        ImageReader.Inst.OnAdded += record => {
            var quad = Instantiate(QuadPrefab.gameObject);
            quad.GetComponent<Renderer>().material.mainTexture = record.Texture;

            quad.transform.position =
                PositionOffset +
                new Vector3((Random.value - 0.5f) * PositionRandomness.x, (Random.value - 0.5f) * PositionRandomness.y, (Random.value - 0.5f) * PositionRandomness.z)
                ;
            quad.transform.rotation =
                Quaternion.Euler((Random.value - 0.5f) * RotationRandomness.x, (Random.value - 0.5f) * RotationRandomness.y, (Random.value - 0.5f) * RotationRandomness.z)
                ;
        };
    }
}
