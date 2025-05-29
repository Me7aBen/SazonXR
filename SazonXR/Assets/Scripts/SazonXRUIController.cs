using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SazonXRUIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject splashPanel;
    public GameObject instructionsPanel;
    public GameObject confirmPanel;
    public GameObject recipeListPanel;
    public GameObject stepPanel;

    [Header("Buttons")]
    public Button startButton;
    public Button captureButton;
    public Button usePhotoButton;
    public Button recipeNextStepButton;
    public Button recipePreviousStepButton;

    [Header("Back Buttons")]
    public Button instructionsBackButton;
    public Button confirmBackButton;

    [Header("Dynamic UI")]
    public RawImage confirmImagePreview;
    public Transform recipeListContainer;
    public GameObject recipeButtonPrefab;

    [Header("Dependencies")]
    public CameraCaptureManager cameraCaptureManager;
    public GeminiFileChatManager geminiManager;
    public SpoonacularRecipeFetcher recipeFetcher;

    private string lastImagePath;

    private void Start()
    {
        // Navigation buttons
        startButton.onClick.AddListener(() => ShowPanel(instructionsPanel));
        captureButton.onClick.AddListener(CaptureImage);
        usePhotoButton.onClick.AddListener(SendImageToGemini);

        instructionsBackButton.onClick.AddListener(() => ShowPanel(splashPanel));
        confirmBackButton.onClick.AddListener(() => ShowPanel(instructionsPanel));

        // Start in splash screen
        ShowPanel(splashPanel);
    }

    private void ShowPanel(GameObject panelToShow)
    {
        splashPanel.SetActive(false);
        instructionsPanel.SetActive(false);
        confirmPanel.SetActive(false);
        recipeListPanel.SetActive(false);
        stepPanel.SetActive(false);

        panelToShow.SetActive(true);
    }

    private void CaptureImage()
    {
        string path = cameraCaptureManager.SaveCurrentCameraFrameToFile();
        if (!string.IsNullOrEmpty(path))
        {
            lastImagePath = path;
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(System.IO.File.ReadAllBytes(path));
            confirmImagePreview.texture = tex;
            ShowPanel(confirmPanel);
        }
        else
        {
            Debug.LogError("Could not capture or save image.");
        }
    }

    private void SendImageToGemini()
    {
        if (string.IsNullOrEmpty(lastImagePath))
        {
            Debug.LogError("⚠No image path stored.");
            return;
        }

        // Delegates responsibility to GeminiFileChatManager
        geminiManager.SendImageToGemini(lastImagePath);
        ShowPanel(recipeListPanel); // We could delay this until recipes arrive
    }

    public void ShowStepPanel()
    {
        ShowPanel(stepPanel);
    }

    public void ShowRecipeListPanel()
    {
        ShowPanel(recipeListPanel);
    }

    public void ShowConfirmPanel(Texture2D image)
    {
        confirmImagePreview.texture = image;
        ShowPanel(confirmPanel);
    }
}