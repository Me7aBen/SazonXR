using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

public class SpoonacularRecipeFetcher : MonoBehaviour
{
    [SerializeField] private string apiKey = "YOUR_SPOONACULAR_API_KEY";
    [SerializeField] private SazonXRUIController uiController;

    private void Start()
    {
        if (uiController == null)
        {
            uiController = FindObjectOfType<SazonXRUIController>();
        }
    }
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
                List<RecipeResult> recipes = JsonConvert.DeserializeObject<List<RecipeResult>>(json);

                Debug.Log($"Received {recipes.Count} recipes.");

                if (uiController == null)
                {
                    Debug.LogError("uiController is still null!");
                }
                else
                {
                    Debug.Log("Calling DisplayRecipes...");
                    uiController.DisplayRecipes(recipes);
                    Debug.Log("DisplayRecipes call completed.");
                }

            }
            catch (Exception ex)
            {
                Debug.LogError("Exception while calling Spoonacular: " + ex.Message);
            }
        }
    }
    
    
    [Serializable]
    public class RecipeInstruction
    {
        public List<RecipeInstructionStep> steps;
    }

    public async void GetRecipeSteps(int recipeId)
    {
        string url = $"https://api.spoonacular.com/recipes/{recipeId}/analyzedInstructions?apiKey={apiKey}";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"Error fetching steps: {response.StatusCode}");
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();
                var instructions = JsonConvert.DeserializeObject<List<RecipeInstruction>>(json);

                if (instructions == null || instructions.Count == 0 || instructions[0].steps.Count == 0)
                {
                    Debug.LogWarning("No steps found for this recipe.");
                    return;
                }

                uiController.LoadRecipeSteps(instructions[0].steps); // ← pasa a UI
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception fetching recipe steps: " + ex.Message);
            }
        }
    }
    
    
    
}


