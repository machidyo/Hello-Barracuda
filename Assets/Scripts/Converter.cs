using Cysharp.Threading.Tasks;
using Unity.Barracuda;
using UnityEngine;

public class Converter : MonoBehaviour
{
    private const int IMAGE_MEAN = 127;     // MEAN
    private const float IMAGE_STD = 127.5f; // STD

    // 画像の前処理
    public static async UniTask<Tensor> ProcessImage(RenderTexture renderTexture, int imageSize)
    {
        var texture = await CropSquare(renderTexture);
        var scaled = Scaled(texture, imageSize, imageSize);
        var tensor = TransformInput(scaled.GetPixels32(), imageSize, imageSize);
        return tensor;
    }

    // 画像の前処理
    public static async UniTask<Tensor> ProcessImage(WebCamTexture webCamTexture, int imageSize)
    {
        var texture = await CropSquare(webCamTexture);
        var scaled = Scaled(texture, imageSize, imageSize);
        var tensor = TransformInput(scaled.GetPixels32(), imageSize, imageSize);
        return tensor;
    }

    // 画像のクロップ（RenderTexture → Texture2D）
    private static async UniTask<Texture2D> CropSquare(RenderTexture texture)
    {
        // Texture2Dの準備
        var smallest = texture.width < texture.height ? texture.width : texture.height;
        Debug.Log($"smallest = {smallest}");
        var rect = new Rect(0, 0, smallest, smallest);
        var result = new Texture2D((int) rect.width, (int) rect.height);

        await UniTask.DelayFrame(1);

        RenderTexture.active = texture;
        result.ReadPixels(rect, 0, 0);
        result.Apply();

        return result;
    }

    // 画像のクロップ（WebCamTexture → Texture2D）
    private static async UniTask<Texture2D> CropSquare(WebCamTexture texture)
    {
        // Texture2Dの準備
        var smallest = texture.width < texture.height ? texture.width : texture.height;
        var rect = new Rect(0, 0, smallest, smallest);
        var result = new Texture2D((int) rect.width, (int) rect.height);

        // 画像のクロップ
        if (rect.width != 0 && rect.height != 0)
        {
            result.SetPixels(texture.GetPixels(
                Mathf.FloorToInt((texture.width - rect.width) / 2),
                Mathf.FloorToInt((texture.height - rect.height) / 2),
                Mathf.FloorToInt(rect.width),
                Mathf.FloorToInt(rect.height)));
            await UniTask.DelayFrame(1);
            result.Apply();
        }

        await UniTask.DelayFrame(1);
        return result;
    }

    // 画像のスケール（Texture2D → Texture2D）
    private static Texture2D Scaled(Texture2D texture, int width, int height)
    {
        // リサイズ後のRenderTextureの生成
        var rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(texture, rt);

        // リサイズ後のTexture2Dの生成
        var preRT = RenderTexture.active;
        RenderTexture.active = rt;
        var ret = new Texture2D(width, height);
        ret.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        ret.Apply();
        RenderTexture.active = preRT;
        RenderTexture.ReleaseTemporary(rt);
        return ret;
    }

    private static Tensor TransformInput(Color32[] pic, int width, int height)
    {
        var floatValues = new float[width * height * 3];
        for (var i = 0; i < pic.Length; ++i)
        {
            var color = pic[i];
            floatValues[i * 3 + 0] = (color.r - IMAGE_MEAN) / IMAGE_STD;
            floatValues[i * 3 + 1] = (color.g - IMAGE_MEAN) / IMAGE_STD;
            floatValues[i * 3 + 2] = (color.b - IMAGE_MEAN) / IMAGE_STD;
        }

        return new Tensor(1, height, width, 3, floatValues);
    }
}
