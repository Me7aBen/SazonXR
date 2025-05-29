
using System.Collections.Generic;
using TMPro;
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

    [Header("Step UI")]
    public TextMeshProUGUI stepNumberText;
    public TextMeshProUGUI stepDescriptionText;

    [Header("Dependencies")]
    public CameraCaptureManager cameraCaptureManager;
    public GeminiFileChatManager geminiManager;
    public SpoonacularRecipeFetcher recipeFetcher;

    private string lastImagePath;

    private List<string> currentSteps;
    private int currentStepIndex = 0;

    private void Start()
    {
        // Navigation buttons
        startButton.onClick.AddListener(() => ShowPanel(instructionsPanel));
        captureButton.onClick.AddListener(CaptureImage);
        usePhotoButton.onClick.AddListener(SendImageToGemini);
        instructionsBackButton.onClick.AddListener(() => ShowPanel(splashPanel));
        confirmBackButton.onClick.AddListener(() => ShowPanel(instructionsPanel));
        recipeNextStepButton.onClick.AddListener(NextStep);
        recipePreviousStepButton.onClick.AddListener(PreviousStep);

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

        geminiManager.SendImageToGemini(lastImagePath);
        ShowPanel(recipeListPanel); // We could delay this until recipes arrive
    }

    public void DisplaySteps(List<string> steps)
    {
        currentSteps = steps;
        currentStepIndex = 0;
        UpdateStepUI();
        ShowPanel(stepPanel);
    }

    private void UpdateStepUI()
    {
        if (currentSteps == null || currentSteps.Count == 0) return;

        stepNumberText.text = $"Step {currentStepIndex + 1}";
        stepDescriptionText.text = currentSteps[currentStepIndex];
    }

    private void NextStep()
    {
        if (currentStepIndex < currentSteps.Count - 1)
        {
            currentStepIndex++;
            UpdateStepUI();
        }
    }

    private void PreviousStep()
    {
        if (currentStepIndex > 0)
        {
            currentStepIndex--;
            UpdateStepUI();
        }
    }
    
    public void DisplayRecipes(List<RecipeResult> recipes)
    {
        foreach (Transform child in recipeListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var recipe in recipes)
        {
            GameObject buttonObj = Instantiate(recipeButtonPrefab, recipeListContainer);
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            buttonText.text = recipe.title;

            Button button = buttonObj.GetComponent<Button>();
            int recipeId = recipe.id;
            button.onClick.AddListener(() => recipeFetcher.GetRecipeSteps(recipeId));
        }

        ShowPanel(recipeListPanel);
    }

    public void LoadRecipeSteps(List<RecipeInstructionStep> steps)
    {
        currentSteps = new List<string>();
        foreach (var step in steps)
        {
            currentSteps.Add(step.step);
        }

        currentStepIndex = 0;
        UpdateStepUI();
        ShowPanel(stepPanel);
    }
    
}
