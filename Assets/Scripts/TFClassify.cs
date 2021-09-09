using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.UI;

public class TFClassify : MonoBehaviour
{
    public const int IMAGE_SIZE = 224;             // model input

    [SerializeField] private NNModel modelFile;
    [SerializeField] private TextAsset labelsFile;
    [SerializeField] private Text output;

    private WebCamera webCamera;

    private IWorker worker;
    private string[] labels;

    private bool isWorking = false;
    
    void Start()
    {
        webCamera = FindObjectOfType<WebCamera>();

        labels = Regex.Split(labelsFile.text, "\n|\r|\r\n")
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();

        var model = ModelLoader.Load(modelFile);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
    }

    async void Update()
    {
        if (isWorking) return;
        isWorking = true;
        
        var input = await Converter.ProcessImage(webCamera.Texture, IMAGE_SIZE); // WebCamTexture
        var results = Predict(input)
            .Take(3)
            .Select(x => $"{x.Key}: {x.Value:0.000}%")
            .ToList();
        output.text = string.Join(Environment.NewLine, results);
        
        input.Dispose();
        isWorking = false;
    }

    private IEnumerable<KeyValuePair<string, float>> Predict(Tensor input)
    {
        var inputs = new Dictionary<string, Tensor> {{"input", input}};
        worker.Execute(inputs);
        // worker.Execute(input); // 入力が一つの場合、どちらでもOK
        var output = worker.PeekOutput("MobilenetV2/Predictions/Reshape_1");
        // Debug.Log($"{output.batch}, {output.dimensions}, {output.height}, {output.width}, {output.channels}");
        // 1, 1, 1, 1, 1001
        return Enumerable.Range(0, output.channels)
            .Select(i => new KeyValuePair<string, float>(labels[i], output[i] * 100))
            // .Select(i => new KeyValuePair<string, float>(labels[i], output[0, 0, 0, i] * 100)) // データ構造的にどちらでも上でもOK
            .OrderByDescending(r => r.Value);
    }
}
