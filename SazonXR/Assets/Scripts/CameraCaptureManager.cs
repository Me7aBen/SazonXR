using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using Uralstech.UXR.QuestCamera;

public class CameraCaptureManager : MonoBehaviour
{
    [Header("UI (Optional)")]
    [SerializeField] private RawImage previewImage;

    private CameraDevice camera;
    private CaptureSessionObject<ContinuousCaptureSession> session;
    private bool isInitialized = false;

    private void Update()
    {
        if (isInitialized && OVRInput.GetDown(OVRInput.RawButton.A))
        {
            StartCoroutine(DelayedCapture());
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);

        if (!CameraSupport.IsSupported)
        {
            Debug.LogError("Passthrough Camera API not supported!");
            yield break;
        }

        if (!Permission.HasUserAuthorizedPermission(UCameraManager.HeadsetCameraPermission))
        {
            Permission.RequestUserPermission(UCameraManager.HeadsetCameraPermission);
            yield break;
        }

        while (UCameraManager.Instance == null)
        {
            Debug.Log("Waiting for UCameraManager...");
            yield return null;
        }

        CameraInfo camInfo = UCameraManager.Instance.GetCamera(CameraInfo.CameraEye.Left);
        if (camInfo == null || camInfo.SupportedResolutions.Length == 0)
        {
            Debug.LogError("No camera info or resolutions found.");
            yield break;
        }

        Resolution res = camInfo.SupportedResolutions[0];
        foreach (Resolution r in camInfo.SupportedResolutions)
        {
            if (r.width * r.height > res.width * res.height)
                res = r;
        }

        camera = UCameraManager.Instance.OpenCamera(camInfo);
        yield return camera.WaitForInitialization();

        if (camera.CurrentState != NativeWrapperState.Opened)
        {
            Debug.LogError("Could not open camera.");
            camera.Destroy();
            yield break;
        }

        session = camera.CreateContinuousCaptureSession(res);
        yield return session.CaptureSession.WaitForInitialization();

        if (session.CaptureSession.CurrentState != NativeWrapperState.Opened)
        {
            Debug.LogError("Could not start capture session.");
            session.Destroy();
            camera.Destroy();
            yield break;
        }

        if (previewImage != null)
            previewImage.texture = session.TextureConverter.FrameRenderTexture;

        isInitialized = true;
        Debug.Log("Camera session initialized.");
    }

    private IEnumerator DelayedCapture()
    {
        Debug.Log("Waiting 0.2s before capturing...");
        yield return new WaitForSeconds(0.2f);

        string path = SaveRenderTextureToPNG(session.TextureConverter.FrameRenderTexture);
        Debug.Log("Captured via A button to path: " + path);
    }

    private string SaveRenderTextureToPNG(RenderTexture rt)
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        RenderTexture.active = currentRT;

        byte[] imageBytes = tex.EncodeToPNG();
        Destroy(tex);

        if (imageBytes == null || imageBytes.Length == 0)
        {
            Debug.LogError("Image bytes are empty!");
            return null;
        }

        string path = Path.Combine(Application.persistentDataPath, "captured_image.png");
        File.WriteAllBytes(path, imageBytes);
        Debug.Log($"Image saved to: {path} ({imageBytes.Length} bytes)");
        return path;
    }
    
    public string SaveCurrentCameraFrameToFile()
    {
        if (!isInitialized || session == null)
        {
            Debug.LogWarning("Camera not initialized or session missing.");
            return null;
        }

        return SaveRenderTextureToPNG(session.TextureConverter.FrameRenderTexture);
    }

    private void OnDestroy()
    {
        session?.Destroy();
        camera?.Destroy();
    }
}