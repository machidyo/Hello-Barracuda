using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebCamera : MonoBehaviour
{
    private RawImage rawImage;
    private WebCamTexture webCamTexture;

    public WebCamTexture Texture => webCamTexture;
    
    void Start ()
    {
        // Webカメラの開始
        rawImage = GetComponent<RawImage>();
        webCamTexture = new WebCamTexture(Detector.IMAGE_SIZE, Detector.IMAGE_SIZE, 30);
        rawImage.texture = webCamTexture;
        webCamTexture.Play();
    }
}
