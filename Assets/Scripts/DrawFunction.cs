using System;
using UnityEngine;

public class DrawFunction : MonoBehaviour
{
    private static Texture2D boxOutlineTexture;
    private static GUIStyle labelStyle;

    static DrawFunction()
    {
        boxOutlineTexture = new Texture2D(1, 1);
        boxOutlineTexture.SetPixel(0, 0, Color.red);
        boxOutlineTexture.Apply();
   
        labelStyle = new GUIStyle();
        labelStyle.fontSize = 50;
        labelStyle.normal.textColor = Color.red;
    }

    public static void DrawBoxOutline(BoundingBox outline, float scaleFactor, float shiftX, float shiftY)
    {
        var x = outline.Dimensions.X * scaleFactor + shiftX;
        var y = outline.Dimensions.Y * scaleFactor + shiftY;
        var width = outline.Dimensions.Width * scaleFactor;
        var height = outline.Dimensions.Height * scaleFactor;
        
        DrawRectangle(new Rect(x, y, width, height), 4);
        DrawLabel(new Rect(x + 10, y + 10, 200, 20), $"{outline.Label}: {(int)outline.Confidence * 100}%");
    }
    
    private static void DrawRectangle(Rect area, int frameWidth)
    {
        var lineAre = area;
        lineAre.height = frameWidth;
        GUI.DrawTexture(lineAre, boxOutlineTexture); // Top line

        lineAre.y = area.yMax - frameWidth;
        GUI.DrawTexture(lineAre, boxOutlineTexture); // Bottom line

        lineAre = area;
        lineAre.width = frameWidth;
        GUI.DrawTexture(lineAre, boxOutlineTexture); // Left line

        lineAre.x = area.xMax - frameWidth;
        GUI.DrawTexture(lineAre, boxOutlineTexture); // Right line
    }

    private static void DrawLabel(Rect position, string text)
    {
        Debug.Log(text);
        GUI.Label(position, text, labelStyle);
    }
}
