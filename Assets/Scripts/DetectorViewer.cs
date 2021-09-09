using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DetectorViewer : MonoBehaviour
{
    private WebCamera webCamera;
    private Detector detector;

    private bool isWorking = false;
    private IList<BoundingBox> boxOutlines;

    private float shiftX;
    private float shiftY;
    private float scaleFactor = 1;

    void Start()
    {
        webCamera = FindObjectOfType<WebCamera>();
        detector = GetComponent<Detector>();

        CalculateShift(Detector.IMAGE_SIZE);
    }

    async void Update()
    {
        if (isWorking) return;
        isWorking = true;
        
        var input = await Converter.ProcessImage(webCamera.Texture, Detector.IMAGE_SIZE); // WebCamTexture
        boxOutlines = detector.Detect(input);
        
        Resources.UnloadUnusedAssets();
        input.Dispose();
        isWorking = false;
    }
    
    public void OnGUI()
    {
        if (boxOutlines != null && boxOutlines.Any())
        {
            foreach (var outline in boxOutlines)
            {
                DrawFunction.DrawBoxOutline(outline, scaleFactor, shiftX, shiftY);
            }
        }
    }

    private void CalculateShift(int imageSize)
    {
        shiftX = imageSize / 2f;
        shiftY = imageSize / 2f;
        scaleFactor = 1;
    }
}
