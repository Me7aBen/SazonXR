using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Uralstech.UGemini;
using Uralstech.UGemini.Models;
using Uralstech.UGemini.Models.Content;
using Uralstech.UGemini.Models.Generation.Chat;
using Newtonsoft.Json;

public class GeminiFileChatManager : MonoBehaviour
{
    public static GeminiFileChatManager Instance;

    [SerializeField] private SpoonacularRecipeFetcher spoonacularFetcher; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public async void SendImageToGemini(string imagePath)
    {
        Debug.Log($"Sending image to Gemini: {imagePath}");

        if (!File.Exists(imagePath))
        {
            Debug.LogError("File does not exist: " + imagePath);
            return;
        }

        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string base64Data = Convert.ToBase64String(imageBytes);

        var content = new GeminiContent()
        {
            Parts = new GeminiContentPart[]
            {
                new GeminiContentPart() { Text = GetPromptWithJSONInstructions() },
                new GeminiContentPart()
                {
                    InlineData = new GeminiContentBlob()
                    {
                        MimeType = GeminiContentType.ImagePNG,
                        Data = base64Data
                    }
                }
            }
        };

        GeminiChatRequest request = new GeminiChatRequest(GeminiModel.Gemini1_5Flash)
        {
            Contents = new GeminiContent[] { content }
        };

        GeminiChatResponse response = await GeminiManager.Instance.Request<GeminiChatResponse>(request);
        string reply = response?.Parts[0].Text;

        Debug.Log("Gemini Response:\n" + reply);

        List<string> ingredients = ExtractIngredientsFromGeminiResponse(reply);
        if (ingredients.Count > 0)
        {
            Debug.Log("Extracted ingredients:");
            foreach (string ingredient in ingredients)
                Debug.Log($"- {ingredient}");

            // 🔗 Llama al API de Spoonacular automáticamente
            spoonacularFetcher.GetRecipesFromIngredients(ingredients);
        }
        else
        {
            Debug.LogWarning("No ingredients could be parsed from the response.");
        }
    }

    private List<string> ExtractIngredientsFromGeminiResponse(string rawResponse)
    {
        try
        {
            string cleanJson = rawResponse;
            Match match = Regex.Match(rawResponse, @"```json\s*(.*?)\s*```", RegexOptions.Singleline);
            if (match.Success)
                cleanJson = match.Groups[1].Value.Trim();

            cleanJson = cleanJson.Replace("“", "\"").Replace("”", "\"");

            return JsonUtilityWrapper.FromJsonArray(cleanJson);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse Gemini response: {ex.Message}");
            return new List<string>();
        }
    }

    private string GetPromptWithJSONInstructions()
    {
        return "What ingredients are in this image?\n\nReturn ONLY a valid JSON array of strings with the detected ingredients. Do not include any explanation or formatting. Example: [\"tomato\", \"carrot\", \"onion\"]";
    }
}

public static class JsonUtilityWrapper
{
    [Serializable]
    private class Wrapper
    {
        public List<string> list;
    }

    public static List<string> FromJsonArray(string json)
    {
        string wrappedJson = $"{{\"list\":{json}}}";
        return JsonUtility.FromJson<Wrapper>(wrappedJson)?.list ?? new List<string>();
    }
}


