using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Uralstech.UGemini;
using Uralstech.UGemini.Models;
using Uralstech.UGemini.Models.Content;
using Uralstech.UGemini.Models.Generation.Chat;

public class GeminiFileChatManager : MonoBehaviour
{
    public static GeminiFileChatManager Instance { get; private set; }

    [TextArea(2, 4)]
    public string defaultPrompt = "What is this ingredient?";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Optional: ensure only one exists
            return;
        }
        Instance = this;
    }

    public async void SendImageToGemini(string path, string userPrompt = null)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"Image file not found at path: {path}");
            return;
        }

        byte[] imageBytes;
        try
        {
            imageBytes = await File.ReadAllBytesAsync(path);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to read file: {ex.Message}");
            return;
        }

        GeminiContent content = new GeminiContent
        {
            Parts = new GeminiContentPart[]
            {
                new GeminiContentPart { Text = userPrompt ?? defaultPrompt },
                new GeminiContentPart
                {
                    InlineData = new GeminiContentBlob
                    {
                        MimeType = GeminiContentType.ImagePNG,
                        Data = Convert.ToBase64String(imageBytes)
                    }
                }
            }
        };

        var request = new GeminiChatRequest(GeminiModel.Gemini1_5Flash)
        {
            Contents = new[] { content }
        };

        try
        {
            GeminiChatResponse response = await GeminiManager.Instance.Request<GeminiChatResponse>(request);
            if (response?.Parts?.Length > 0)
            {
                Debug.Log($"Gemini Response: {response.Parts[0].Text}");
            }
            else
            {
                Debug.LogWarning("Empty response from Gemini.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Gemini API request failed: {ex.Message}");
        }
    }
}