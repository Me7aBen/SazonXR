using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using Uralstech.UXR.QuestCamera;

public class CameraTest : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;

    private CameraDevice camera;
    private CaptureSessionObject<ContinuousCaptureSession> sessionObject;
    private bool isCapturing = false;

    private void Update()
    {
        // Detectar si se presiona el botón A del control derecho
        if (!isCapturing && OVRInput.GetDown(OVRInput.RawButton.A))
        {
            Debug.Log("[CameraButtonTrigger] A button pressed – starting capture.");
            isCapturing = true;
            StartCoroutine(InitializeCameraAndCapture());
        }
    }

    private IEnumerator InitializeCameraAndCapture()
    {
        if (!CameraSupport.IsSupported)
        {
            Debug.LogError("Device does not support the Passthrough Camera API!");
            yield break;
        }

        if (!Permission.HasUserAuthorizedPermission(UCameraManager.HeadsetCameraPermission))
        {
            Permission.RequestUserPermission(UCameraManager.HeadsetCameraPermission);
            yield break;
        }

        CameraInfo currentCamera = UCameraManager.Instance.GetCamera(CameraInfo.CameraEye.Left);

        Resolution highestResolution = currentCamera.SupportedResolutions[0];
        foreach (Resolution resolution in currentCamera.SupportedResolutions)
        {
            if (resolution.width * resolution.height > highestResolution.width * highestResolution.height)
                highestResolution = resolution;
        }

        camera = UCameraManager.Instance.OpenCamera(currentCamera);
        yield return camera.WaitForInitialization();

        if (camera.CurrentState != NativeWrapperState.Opened)
        {
            Debug.LogError("Could not open camera!");
            camera.Destroy();
            yield break;
        }

        sessionObject = camera.CreateContinuousCaptureSession(highestResolution);
        yield return sessionObject.CaptureSession.WaitForInitialization();

        if (sessionObject.CaptureSession.CurrentState != NativeWrapperState.Opened)
        {
            Debug.LogError("Could not open camera session!");
            sessionObject.Destroy();
            camera.Destroy();
            yield break;
        }

        if (rawImage != null)
            rawImage.texture = sessionObject.TextureConverter.FrameRenderTexture;

        Debug.Log("[CameraButtonTrigger] Image capture started and displaying.");
    }

    private void OnDestroy()
    {
        sessionObject?.Destroy();
        camera?.Destroy();
    }
}