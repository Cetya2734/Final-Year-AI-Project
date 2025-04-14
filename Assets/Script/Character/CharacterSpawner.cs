using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    public GridManager gridManager;
    public ResourceManager resourceManager;
    public GameObject characterPrefab;

    private List<GridCell> occupiedCells = new List<GridCell>(); // Track occupied cells
    private bool isSpawning = false; // Flag to track if the coroutine is running
    private float lastSpawnTime = 0f; // Track the last spawn time
    private float spawnCooldown = 1f; // Cooldown duration in seconds

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
            int activeCharacters = GameObject.FindGameObjectsWithTag("Player").Length;
            int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

            // Adaptive spawn delay
            float spawnDelay = enemyCount > activeCharacters ? 1.5f : 3f; // Slower if outnumbered
            spawnDelay += Mathf.Clamp(activeCharacters * 0.5f, 0, 5); // Scale delay with unit count

            if (currentResources >= 4 && Time.time - lastSpawnTime >= spawnCooldown && !isSpawning)
            {
                yield return StartCoroutine(SpawnCharacter());
                lastSpawnTime = Time.time; // Update the last spawn time
            }
            else
            {
                Debug.Log("Not enough resources or cooldown not met, delaying for 3 seconds.");
                yield return new WaitForSeconds(3);
            }

            yield return new WaitForSeconds(spawnDelay); // Dynamic wait time
        }
    }

    private IEnumerator SpawnCharacter()
    {
        if (isSpawning) yield break; // Prevent re-entry
        isSpawning = true;

        List<GridCell> spawnableCells = gridManager.GetSpawnableCells();
        spawnableCells.RemoveAll(cell => occupiedCells.Contains(cell));

        if (spawnableCells.Count == 0)
        {
            Debug.LogWarning("All spawnable cells are occupied!");
            isSpawning = false;
            yield break;
        }

        GridCell bestCell = GetBestSpawnLocation(spawnableCells);
        if (bestCell == null)
        {
            Debug.LogWarning("No optimal spawn found, choosing random.");
            bestCell = spawnableCells[Random.Range(0, spawnableCells.Count)];
        }

        Vector3 spawnPosition = new Vector3(bestCell.GridPosition.x, bestCell.GridPosition.y, 0);
        GameObject newCharacter = Instantiate(characterPrefab, spawnPosition, Quaternion.identity);
        occupiedCells.Add(bestCell);

        resourceManager.ConsumeResources(4);

        isSpawning = false;
        yield return null; // Ensure the coroutine completes
    }

    private GridCell GetBestSpawnLocation(List<GridCell> spawnableCells)
    {
        GameObject[] allies = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        GridCell bestCell = null;
        float bestScore = float.MinValue;

        foreach (GridCell cell in spawnableCells)
        {
            float score = 0;

            // Prefer locations near allies
            foreach (GameObject ally in allies)
            {
                float distance = Vector3.Distance(ally.transform.position, new Vector3(cell.GridPosition.x, cell.GridPosition.y, 0));
                score += 10 / (distance + 1); // Higher score for closer allies
            }

            // Avoid locations near enemies
            foreach (GameObject enemy in enemies)
            {
                float distance = Vector3.Distance(enemy.transform.position, new Vector3(cell.GridPosition.x, cell.GridPosition.y, 0));
                score -= 15 / (distance + 1); // Lower score for closer enemies
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        return bestCell;
    }
}
