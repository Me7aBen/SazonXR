using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public class SpoonacularRecipeFetcher : MonoBehaviour
{
    [SerializeField] private string apiKey = "YOUR_SPOONACULAR_API_KEY"; 

    public async void GetRecipesFromIngredients(List<string> ingredients)
    {
        if (ingredients == null || ingredients.Count == 0)
        {
            Debug.LogWarning("No ingredients provided.");
            return;
        }

        string ingredientsQuery = string.Join(",", ingredients).Replace(" ", "+");
        string url = $"https://api.spoonacular.com/recipes/findByIngredients?ingredients={ingredientsQuery}&number=3&ranking=1&ignorePantry=true&apiKey={apiKey}";

        Debug.Log("🔍 Requesting recipes from Spoonacular...");
        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"Spoonacular API error: {response.StatusCode}");
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();
                Debug.Log("Recipes found:\n" + json);
                
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception while calling Spoonacular: " + ex.Message);
            }
        }
    }
}