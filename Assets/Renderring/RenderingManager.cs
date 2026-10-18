using UnityEngine;

public class RenderTextureResolutionScaler : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Camera renderingCamera;
    [SerializeField] private RenderTexture renderTexture;

    Vector2Int gameResolutionOnLastFrame;

    private void Update()
    {
        Vector2Int gameResolution = GetGameResolution();
        if (gameResolutionOnLastFrame != gameResolution)
        {
            Debug.Log("Game resolution changed to :" + gameResolution);

            gameCamera.targetTexture = null;
            ResizeRenderTexture(renderTexture, gameResolution.x, gameResolution.y);
            gameCamera.targetTexture = renderTexture;
        }
    }

    private static Vector2Int GetGameResolution()
    {
        return new Vector2Int(Screen.width, Screen.height);
    }

    private static Vector2Int GetMonitorResolution()
    {
        Resolution resolution = Screen.currentResolution;
        return new Vector2Int(resolution.width, resolution.height);
    }

    private static bool ResizeRenderTexture(RenderTexture renderTexture, int width, int height)
    {
        if (renderTexture == null)
        {
            Debug.LogError("RenderTexture is null");
            return false;
        }

        if (width <= 0 || height <= 0)
        {
            Debug.LogError("Width and height is negative");
            return false;
        }

        renderTexture.Release();
        renderTexture.width = width;
        renderTexture.height = height;
        renderTexture.Create();

        return true;
    }
}