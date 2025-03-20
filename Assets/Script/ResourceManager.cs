using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
    public int maxResources = 10;  // Maximum amount of resources (e.g., 10 units)
    private int currentResources;   // Current available resources

    public int spawnCost = 2;       // Resource cost to spawn one character

    public Text resourceText;       // UI Text to display resources
    public Image resourceBar;       // UI Image to represent the resource bar (fill amount)

    public bool enableRegeneration = true; // Toggle resource regeneration
    public float regenerationInterval = 2f; // Time in seconds to regenerate 1 resource

    private void Start()
    {
        currentResources = maxResources; // Initialize resources
        UpdateUI();

        // Start regeneration if enabled
        if (enableRegeneration)
        {
            StartCoroutine(RegenerateResources());
        }
    }

    // Function to spawn a character (call this from GridManager or UI)
    public bool TrySpawnCharacter(Vector2Int gridPosition, GridManager gridManager)
    {
        // Validate the spawn cell before decrementing resources
        GridCell cell = gridManager.GetCell(gridPosition);
        if (cell == null || !cell.IsWalkable || !cell.IsCharacterSpawnable)
        {
            Debug.LogWarning($"Cannot spawn a character at {gridPosition}. Cell is not valid or spawnable.");
            return false; // Do not decrease resources
        }

        // Check if there are enough resources
        if (ConsumeResources(spawnCost))
        {
            Debug.Log($"Character spawned. Remaining resources: {currentResources}");
            return true;
        }

        Debug.LogWarning("Not enough resources to spawn a character.");
        return false;
    }

    // Function to check the current resource amount
    public int GetCurrentResources()
    {
        return currentResources;
    }

    // Function to update the UI
    private void UpdateUI()
    {
        if (resourceText != null)
        {
            resourceText.text = $"Resources: {currentResources}/{maxResources}";
        }

        if (resourceBar != null)
        {
            resourceBar.fillAmount = (float)currentResources / maxResources; // Update the fill amount of the resource bar
        }
    }

    // Function to consume resources
    public bool ConsumeResources(int amount)
    {
        if (currentResources >= amount)
        {
            currentResources -= amount;
            UpdateUI();
            return true; // Successfully consumed resources
        }
        return false; // Not enough resources
    }

    // Optional: If you want to add resources over time, you can create a regeneration function
    public void RegenerateResources(int amount)
    {
        currentResources = Mathf.Min(currentResources + amount, maxResources); // Ensure it doesn't exceed max resources
        UpdateUI();
    }

    private IEnumerator RegenerateResources()
    {
        while (enableRegeneration)
        {
            if (currentResources < maxResources)
            {
                currentResources = Mathf.Min(currentResources + 1, maxResources); // Increase resources by 1
                UpdateUI();
            }

            yield return new WaitForSeconds(regenerationInterval); // Wait for the regeneration interval
        }
    }

    // Function to manually add resources
    public void AddResources(int amount)
    {
        currentResources = Mathf.Clamp(currentResources + amount, 0, maxResources); // Ensure it doesn't exceed max resources
        UpdateUI();
    }
}