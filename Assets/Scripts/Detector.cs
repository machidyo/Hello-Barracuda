using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Barracuda;
using UnityEngine;

public class Detector : MonoBehaviour
{
    public const int IMAGE_SIZE = 416;             // model input
    
    private const int ROW_COUNT = 13;              // model output
    private const int COL_COUNT = 13;              // model output
    private const int BOXES_PER_CELL = 5;          // model output: channel = BOXES_PER_CELL * (CLASS_COUNT + BOX_INFO_FEATURE_COUNT)
    private const int BOX_INFO_FEATURE_COUNT = 5;  // ↑
    private const int CLASS_COUNT = 20;            // ↑
    private const float CELL_WIDTH = 32;
    private const float CELL_HEIGHT = 32;
    private readonly float[] anchors = {1.08f, 1.19f, 3.42f, 6.63f, 11.38f, 9.42f, 5.11f, 16.62f, 10.52f};

    [SerializeField] private NNModel modelFile;
    [SerializeField] private TextAsset labelsFile;

    private IWorker worker;
    private string[] labels;

    void Start()
    {
        labels = Regex.Split(labelsFile.text, "\n|\r|\r\n")
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
        var model = ModelLoader.Load(modelFile);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
    }

    public IList<BoundingBox> Detect(Tensor input)
    {
        worker.Execute(input);
        var output = worker.PeekOutput();
        var results = ParseOutputs(output);
        var boxes = FilterBoundingBoxes(results, 5, 0.3f);

        return boxes;
    }

    private IList<BoundingBox> ParseOutputs(Tensor yoloModelOutput, float threshold = .3f)
    {
        var boxes = new List<BoundingBox>();
        for (var cy = 0; cy < COL_COUNT; cy++)
        {
            for (var cx = 0; cx < ROW_COUNT; cx++)
            {
                for (var box = 0; box < BOXES_PER_CELL; box++)
                {
                    var channel = box * (CLASS_COUNT + BOX_INFO_FEATURE_COUNT);
                    var bbd = ExtractBoundingBoxDimensions(yoloModelOutput, cx, cy, channel);
                    var confidence = GetConfidence(yoloModelOutput, cx, cy, channel);

                    if (confidence > 0.01)
                    {
                        Debug.Log($"confidence = {confidence}");
                    }
                    if (confidence < threshold) continue;

                    var predictedClasses = ExtractClasses(yoloModelOutput, cx, cy, channel);
                    var (topResultIndex, topResultScore) = GetTopResult(predictedClasses);
                    var topScore = topResultScore * confidence;
                    
                    Debug.Log($"topScore = {topScore}");
                    if (topScore < threshold) continue;

                    var mappedBoundingBox = MapBoundingBoxToCell(cx, cy, box, bbd);
                    boxes.Add(new BoundingBox
                    {
                        Dimensions = new BoundingBoxDimensions
                        {
                            X = (mappedBoundingBox.X - mappedBoundingBox.Width) / 2,
                            Y = (mappedBoundingBox.Y - mappedBoundingBox.Height) / 2,
                            Width = mappedBoundingBox.Width,
                            Height = mappedBoundingBox.Height
                        },
                        Confidence = confidence,
                        Label = labels[topResultIndex]
                    });
                }
            }
        }
        Debug.Log($"boxes.Count = {boxes.Count}");
        return boxes;
    }

    private BoundingBoxDimensions ExtractBoundingBoxDimensions(Tensor modelOutput, int x, int y, int channel)
    {
        return new BoundingBoxDimensions
        {
            X = modelOutput[0, x, y, channel],
            Y = modelOutput[0, x, y, channel + 1],
            Width = modelOutput[0, x, y, channel + 2],
            Height = modelOutput[0, x, y, channel + 3]
        };
    }

    private float GetConfidence(Tensor modelOutput, int x, int y, int channel)
    {
        return MathFunction.Sigmoid(modelOutput[0, x, y, channel + 4]);
    }

    private float[] ExtractClasses(Tensor modelOutput, int x, int y, int channel)
    {
        var predictedClasses = new float[CLASS_COUNT];
        var predictedClassOffset = channel + BOX_INFO_FEATURE_COUNT;

        for (var predictedClass = 0; predictedClass < CLASS_COUNT; predictedClass++)
        {
            predictedClasses[predictedClass] = modelOutput[0, x, y, predictedClass + predictedClassOffset];
        }

        return MathFunction.Softmax(predictedClasses);
    }

    private ValueTuple<int, float> GetTopResult(float[] predicatedClasses)
    {
        return predicatedClasses
            .Select((predicatedClass, index) => (Index: index, Value: predicatedClass))
            .OrderByDescending(result => result.Value)
            .First();
    }

    private CellDimensions MapBoundingBoxToCell(int x, int y, int box, BoundingBoxDimensions boxDimensions)
    {
        return new CellDimensions
        {
            X = (y + MathFunction.Sigmoid(boxDimensions.X)) * CELL_WIDTH,
            Y = (x + MathFunction.Sigmoid(boxDimensions.Y)) * CELL_HEIGHT,
            Width = (float)Math.Exp(boxDimensions.Width) * CELL_WIDTH + anchors[box * 2],
            Height = (float)Math.Exp(boxDimensions.Height) * CELL_HEIGHT + anchors[box * 2 + 1]
        };
    }
    
    private IList<BoundingBox> FilterBoundingBoxes(IList<BoundingBox> boxes, int limit, float threshold)
    {
        var activeCount = boxes.Count;
        var isActiveBoxes = new bool[boxes.Count];
        for (var i = 0; i < isActiveBoxes.Length; i++)
        {
            isActiveBoxes[i] = true;
        }

        var sortedBoxes = boxes
            .Select((b, i) => new {Box = b, Index = i})
            .OrderByDescending(b => b.Box.Confidence)
            .ToList();
        
        var results = new List<BoundingBox>();
        for (var i = 0; i < boxes.Count; i++)
        {
            if (isActiveBoxes[i])
            {
                var boxA = sortedBoxes[i].Box;
                results.Add(boxA);
                
                if (results.Count >= limit) break;

                for (var j = i + 1; j < boxes.Count; j++)
                {
                    if (isActiveBoxes[j])
                    {
                        var boxB = sortedBoxes[j].Box;
                        if (IntersectionOverUnion(boxA.Rect, boxB.Rect) > threshold)
                        {
                            isActiveBoxes[j] = false;
                            activeCount--;

                            if (activeCount <= 0) break;
                        }
                    }
                }
                
                if (activeCount <= 0) break;
            }
        }
        return results;
    }

    private float IntersectionOverUnion(Rect boundingBoxA, Rect boundingBoxB)
    {
        var areaA = boundingBoxA.width * boundingBoxA.height;
        if (areaA <= 0) return 0;

        var areaB = boundingBoxB.width * boundingBoxB.height;
        if (areaB <= 0) return 0;

        var minX = Math.Max(boundingBoxA.xMin, boundingBoxB.xMin);
        var minY = Math.Max(boundingBoxA.yMin, boundingBoxB.yMin);
        var maxX = Math.Min(boundingBoxA.xMax, boundingBoxB.xMax);
        var maxY = Math.Min(boundingBoxA.yMax, boundingBoxB.yMax);
        var intersectionArea = Math.Max(maxY - minY, 0) * Math.Max(maxX - minX, 0);

        return intersectionArea / (areaA + areaB - intersectionArea);
    }
}

public class DimensionsBase
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Height { get; set; }
    public float Width { get; set; }
}

public class BoundingBoxDimensions : DimensionsBase {}

class CellDimensions : DimensionsBase {}

public class BoundingBox
{
    public BoundingBoxDimensions Dimensions { get; set; }
    public string Label { get; set; }
    public float Confidence { get; set; }
    public Rect Rect => new Rect(Dimensions.X, Dimensions.Y, Dimensions.Width, Dimensions.Height);
    public string ToString => $"{Label}:{Confidence}, {Dimensions.X}:{Dimensions.Y} - {Dimensions.Width}:{Dimensions.Height}";
}
