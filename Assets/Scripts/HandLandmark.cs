using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Barracuda;

public class HandLandmark : MonoBehaviour
{
    public const int IMAGE_SIZE = 224;             // model input
    
    [SerializeField] private NNModel modelAsset;

    [SerializeField] private RenderTexture outputTexture;
    [SerializeField] private RenderTexture outputTexture1;
    [SerializeField] private RenderTexture outputTexture2;

    private WebCamera webCamera;

    private Model runtimeModel;
    private IWorker worker;

    async void Start()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        var workerType = WorkerFactory.Type.Compute; // GPU
        worker = WorkerFactory.CreateWorker(workerType, runtimeModel);

        webCamera = FindObjectOfType<WebCamera>();

        // Debug.Log("適当1秒待ち");
        // await UniTask.Delay(1000);
    }

    async void Update()
    {
        var input = await Converter.ProcessImage(webCamera.Texture, IMAGE_SIZE); // WebCamTexture
//        Debug.Log($"dim = {input.dimensions}");
        Inference(input);
        input.Dispose();
    }

    void OnDestroy()
    {
        worker.Dispose();
    }

    private void Inference(Tensor input)
    {
        var inputs = new Dictionary<string, Tensor> {{"input_1", input}};
        worker.Execute(inputs);
        var output = worker.PeekOutput("Identity");
        var output1 = worker.PeekOutput("Identity_1");
        var output2 = worker.PeekOutput("Identity_2");
        Debug.Log($"{output.dimensions}, {output.length}, {output.data}");
        Debug.Log($"{output1.dimensions}, {output1.length}, {output1.data}");
        Debug.Log($"{output2.dimensions}, {output2.length}, {output2.data}");
        // 意味なし
        // output.ToRenderTexture(outputTexture, 0, 0, 1 / 255f); // 正規化
        // output1.ToRenderTexture(outputTexture1, 0, 0, 1 / 255f); // 正規化
        // output2.ToRenderTexture(outputTexture2, 0, 0, 1 / 255f); // 正規化

        var offset = 0;
        var sharedAccess = output.data.SharedAccess(out offset);
        var sharedAccess1 = output1.data.SharedAccess(out offset);
        var sharedAccess2 = output2.data.SharedAccess(out offset);
        Debug.Log(string.Join(", ", sharedAccess));
        Debug.Log(string.Join(", ", sharedAccess1));
        Debug.Log(string.Join(", ", sharedAccess2));
        
        output.Dispose();
    }
}
