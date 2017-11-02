using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoToImage : MonoBehaviour {

    RawImage image;
    VideoPlayer video;
    
    void Start() {
        image = GetComponent<RawImage>();
        video = GetComponent<VideoPlayer>();

        var tex = new RenderTexture((int)video.clip.width, (int)video.clip.height, 0);
        video.targetTexture = tex;
        image.texture = tex;
    }
}
