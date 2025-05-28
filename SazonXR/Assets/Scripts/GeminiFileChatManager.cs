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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public async void SendImageToGemini(string imagePath, string userPrompt)
    {
        Debug.Log($" Sending image to Gemini: {imagePath}");

        if (!File.Exists(imagePath))
        {
            Debug.LogError(" File does not exist: " + imagePath);
            return;
        }

        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string base64Data = System.Convert.ToBase64String(imageBytes);

        var content = new GeminiContent()
        {
            Parts = new GeminiContentPart[]
            {
                new GeminiContentPart() { Text = GetPromptWithJSONInstructions(userPrompt) },
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

        Debug.Log(" Gemini Response:\n" + reply);
        
        if (!string.IsNullOrEmpty(reply))
        {
            string cleanedReply = reply.Trim();

            cleanedReply = Regex.Replace(cleanedReply, @"^[`´]{3}|[`´]{3}$", ""); // Remueve ```, ´´´ al inicio y al final
            cleanedReply = cleanedReply.Trim('\"'); // Por si acaso lo envolvió en comillas

            try
            {
                var ingredients = JsonConvert.DeserializeObject<List<string>>(cleanedReply);
                Debug.Log("Ingredientes parseados:");
                foreach (var ing in ingredients)
                    Debug.Log("- " + ing);

            }
            catch (System.Exception ex)
            {
                Debug.LogError("No se pudo parsear como JSON: " + ex.Message + "\nTexto: " + cleanedReply);
            }
        }


    }

    private string GetPromptWithJSONInstructions(string prompt)
    {
        return $"{prompt}\n\nReturn ONLY a valid JSON array of strings with the detected ingredients. Do not include any explanation or formatting. Example: [\"tomato\", \"carrot\", \"onion\"]";
    }
}