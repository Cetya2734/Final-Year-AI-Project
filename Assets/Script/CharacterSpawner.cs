using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    public GridManager gridManager;
    public ResourceManager resourceManager;
    public GameObject characterPrefab;

    private List<GridCell> occupiedCells = new List<GridCell>(); // Track occupied cells

    private void Start()
    {
        StartCoroutine(AutoSpawnCharacter());
    }

    private IEnumerator AutoSpawnCharacter()
    {
        while (true)
        {
            if (resourceManager == null)
            {
                Debug.LogError("ResourceManager is NULL in CharacterSpawner!");
                yield break;
            }

            int currentResources = resourceManager.GetCurrentResources();

            if (currentResources >= 4)
            {
                Debug.Log("Spawning immediately! Resources: " + currentResources);
                SpawnCharacter();
            }
            else
            {
                Debug.Log("Not enough resources, delaying for 3 seconds.");
                yield return new WaitForSeconds(3);
                SpawnCharacter();
            }

            yield return new WaitForSeconds(1);
        }
    }

    private void SpawnCharacter()
    {
        List<GridCell> spawnableCells = gridManager.GetSpawnableCells();

        if (spawnableCells.Count == 0)
        {
            Debug.LogWarning("No valid spawnable cells available!");
            return;
        }

        // Filter out occupied cells
        spawnableCells.RemoveAll(cell => occupiedCells.Contains(cell));

        if (spawnableCells.Count == 0)
        {
            Debug.LogWarning("All spawnable cells are occupied!");
            return;
        }

        GridCell randomCell = spawnableCells[Random.Range(0, spawnableCells.Count)];
        Vector3 spawnPosition = new Vector3(randomCell.GridPosition.x, randomCell.GridPosition.y, 0);

        GameObject newCharacter = Instantiate(characterPrefab, spawnPosition, Quaternion.identity);
        occupiedCells.Add(randomCell); // Mark as occupied

        // Deduct resources
        resourceManager.ConsumeResources(4);

        Debug.Log("Character spawned at: " + spawnPosition);
    }
}