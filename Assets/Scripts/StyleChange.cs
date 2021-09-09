using UnityEngine;
using Unity.Barracuda;

public class StyleChange : MonoBehaviour
{
    public const int IMAGE_SIZE = 224;             // model input

    [SerializeField] private NNModel modelAsset;

    //[SerializeField] private RenderTexture inputTexture;
    [SerializeField] private RenderTexture outputTexture;

    private WebCamera webCamera;

    private Model runtimeModel;
    private IWorker worker;

    void Start()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        var workerType = WorkerFactory.Type.Compute;   // GPU
        // var workerType = WorkerFactory.Type.CSharp; // CPU
        worker = WorkerFactory.CreateWorker(workerType, runtimeModel);

        webCamera = FindObjectOfType<WebCamera>();
    }

    async void Update()
    {
        // var input = new Tensor(inputTexture);                            // 元々のコード、エラー出る
        // var input = await Converter.ProcessImage(inputTexture, IMAGE_SIZE);          // RenderTexture
        var input = await Converter.ProcessImage(webCamera.Texture, IMAGE_SIZE); // WebCamTexture
        Inference(input);
        input.Dispose();
    }
    
    void OnDestroy()
    {
        worker.Dispose();
    }

    private void Inference(Tensor input)
    {
        worker.Execute(input);
        var output = worker.PeekOutput();
        output.ToRenderTexture(outputTexture, 0, 0, 1 / 255f); // 正規化
        output.Dispose(); // 各STEP毎にTensorを破棄しないとメモリリークの恐れあり
    }
}
