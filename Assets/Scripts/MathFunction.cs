using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MathFunction 
{
    public static float Sigmoid(float value)
    {
        var k = (float) Math.Exp(value);
        return k / (1.0f + k);
    }

    public static float[] Softmax(float[] values)
    {
        var maxValue = values.Max();
        var exp = values.Select(v => Math.Exp(v - maxValue)).ToList();
        var sumExp = exp.Sum();
        return exp.Select(v => (float)(v / sumExp)).ToArray();
    }
}
