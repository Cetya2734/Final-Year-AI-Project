using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class GridManager : MonoBehaviour
{
    public int GridSize = 10;          // 10x10 grid
    public float CellSize = 1f;       // Size of each cell
    public GameObject CellPrefab;     // Prefab for visual representation of a cell
    public GameObject CharacterPrefab; // Prefab of the characters
    public GameObject EnemyPrefab; // Prefab of the enemy to spawn
    public GameObject PatrolEnemyPrefab;

    public PathFindingManager pathfindingManager;

    public ResourceManager resourceManager; // Reference to the ResourceManager

    private GridCell[,] grid;         // 2D array of GridCells

    private List<GameObject> characters = new List<GameObject>();
    public Vector2Int targetPosition = new Vector2Int(5, 9); // Target position for the character to move to

    private void Start()
    {
        CreateGrid();

        // Set multiple cells as not walkable
        foreach (var position in blockedCells)
        {
            UpdateCell(position, false, 999); // Mark as not walkable with a high movement cost
        }

        // Mark specific cells as not spawnable for characters
        foreach (var position in notSpawnableForCharacterCells)
        {
            UpdateCell(position, true, 1, isCharacterSpawnable: false);
        }

        // Link the PathfindingManager
        if (pathfindingManager == null)
        {
            pathfindingManager = FindObjectOfType<PathFindingManager>();
        }

        SpawnEnemy(new Vector2Int(1, 8));
        SpawnEnemy(new Vector2Int(4, 9));
        SpawnEnemy(new Vector2Int(8, 8));
        SpawnEnemy(new Vector2Int(5, 9));

        // List of Patrol points
        List<Vector2Int> patrolPoints1 = new List<Vector2Int> { new Vector2Int(2, 7), new Vector2Int(0, 7), new Vector2Int(1, 6), new Vector2Int(4, 7), new Vector2Int(2, 9) };
        List<Vector2Int> patrolPoints2 = new List<Vector2Int> { new Vector2Int(7, 7), new Vector2Int(9, 7), new Vector2Int(8, 6), new Vector2Int(5, 7), new Vector2Int(7, 9) };

        // Spawn Patrol and Attack Enemies
        SpawnPatrolAndAttackEnemy(new Vector2Int(2, 7), patrolPoints1);
        SpawnPatrolAndAttackEnemy(new Vector2Int(7, 7), patrolPoints2);

        // Start continuous spawning of characters based on resources
        StartCoroutine(SpawnCharactersOverTime());

    }

    public Vector2Int GetRandomSpawnableTile()
    {
        List<Vector2Int> spawnableTiles = new List<Vector2Int>();

        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                if (grid[x, y].IsCharacterSpawnable && grid[x, y].IsWalkable)
                {
                    spawnableTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        if (spawnableTiles.Count > 0)
        {
            return spawnableTiles[Random.Range(0, spawnableTiles.Count)];
        }

        Debug.LogWarning("No valid spawnable tiles found!");
        return Vector2Int.zero;
    }

    private IEnumerator SpawnCharactersOverTime()
    {
        while (true) // Infinite loop to keep checking resources
        {
            if (resourceManager.currentResources > 0) // If resources are available, spawn a character
            {
                List<GridCell> spawnableCells = GetSpawnableCells();

                if (spawnableCells.Count > 0)
                {
                    GridCell spawnCell = spawnableCells[Random.Range(0, spawnableCells.Count)];
                    Vector2Int spawnPosition = spawnCell.GridPosition;

                    SpawnAndMoveCharacter(spawnPosition, targetPosition);
                    resourceManager.ConsumeResources(3); // Deduct resource

                    Debug.Log("Character spawned! Remaining resources: " + resourceManager.currentResources);
                }
            }

            // If resources are below 5, wait 5 seconds, otherwise wait 2 seconds
            if (resourceManager.currentResources < 5)
            {
                Debug.Log("Low resources! Delaying next spawn for 5 seconds...");
                yield return new WaitForSeconds(5f);
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }
        }
    }

    //private void SpawnRandomCharacter()
    //{
    //    if (resourceManager.currentResources > 0) // Only spawn if resources are available
    //    {
    //        List<GridCell> spawnableCells = GetSpawnableCells();

    //        if (spawnableCells.Count > 0)
    //        {
    //            GridCell spawnCell = spawnableCells[Random.Range(0, spawnableCells.Count)];
    //            Vector2Int spawnPosition = spawnCell.GridPosition;

    //            SpawnAndMoveCharacter(spawnPosition, targetPosition);
    //            resourceManager.ConsumeResources(1); // Deduct resource for each spawned character
    //        }
    //        else
    //        {
    //            Debug.LogWarning("No valid spawnable cells available for character spawning.");
    //        }
    //    }
    //}

    // Character respawn logic
    public void RespawnCharacter()
    {
        float delay = (resourceManager.currentResources > 4) ? 1f : 3f;
        StartCoroutine(DelayedRespawn(delay));
    }

    private IEnumerator DelayedRespawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(SpawnCharactersOverTime());
    }

    public void SpawnEnemy(Vector2Int gridPosition)
    {
        GridCell spawnCell = GetCell(gridPosition);

        if (spawnCell != null && spawnCell.IsWalkable && spawnCell.IsEnemySpawnable) // Ensure the cell is walkable and spawnable for enemies
        {
            // Get the world position of the grid cell
            Vector3 worldPosition = spawnCell.CellObject.transform.position;

            // Instantiate the enemy at the world position
            GameObject enemy = Instantiate(EnemyPrefab, worldPosition, Quaternion.identity);

            Debug.Log($"Enemy spawned at {worldPosition}");
        }
        else
        {
            Debug.LogWarning($"Cell at {gridPosition} is not spawnable for enemies.");
        }
    }

    public void SpawnPatrolAndAttackEnemy(Vector2Int gridPosition, List<Vector2Int> patrolPoints)
    {
        // Get the cell at the specified grid position
        GridCell spawnCell = GetCell(gridPosition);

        // Check if the cell exists, is walkable, and allows enemy spawning
        if (spawnCell != null && spawnCell.IsWalkable && spawnCell.IsEnemySpawnable)
        {
            // Get the world position of the cell's visual representation
            Vector3 worldPosition = spawnCell.CellObject.transform.position;

            // Ensure the PatrolEnemyPrefab is assigned
            if (PatrolEnemyPrefab != null)
            {
                // Instantiate the enemy prefab at the world position with default rotation
                GameObject enemy = Instantiate(PatrolEnemyPrefab, worldPosition, Quaternion.identity);

                // Try to get the PatrolAndAttackEnemy script/component from the instantiated enemy
                PatrolAndAttackEnemy patrolAndAttackEnemy = enemy.GetComponent<PatrolAndAttackEnemy>();
                if (patrolAndAttackEnemy != null)
                {
                    // Assign patrol points to the enemy's script
                    patrolAndAttackEnemy.patrolPoints = patrolPoints;
                }

                Debug.Log($"Patrol and Attack Enemy spawned at {worldPosition} with patrol points: {string.Join(", ", patrolPoints)}");
            }
            else
            {
                Debug.LogError("PatrolEnemyPrefab is not assigned in GridManager!");
            }
        }
        else
        {
            Debug.LogWarning($"Cell at {gridPosition} is not spawnable for Patrol and Attack Enemy.");
        }
    }

    private Vector2Int[] blockedCells =
    {
        new Vector2Int(0, 4), new Vector2Int(2, 4), new Vector2Int(3,4), new Vector2Int(4, 4), new Vector2Int(5,4), new Vector2Int(6,4), new Vector2Int(7,4), new Vector2Int(9,4)
    };

    private Vector2Int[] notSpawnableForCharacterCells =
    {
        new Vector2Int(0, 5), new Vector2Int(1, 5), new Vector2Int(2, 5), new Vector2Int(3, 5), new Vector2Int(4, 5), new Vector2Int(5, 5), new Vector2Int(6, 5), new Vector2Int(7, 5), new Vector2Int(8, 5), new Vector2Int(9, 5),
        new Vector2Int(0, 6), new Vector2Int(1, 6), new Vector2Int(2, 6), new Vector2Int(3, 6), new Vector2Int(4, 6), new Vector2Int(5, 6), new Vector2Int(6, 6), new Vector2Int(7, 6), new Vector2Int(8, 6), new Vector2Int(9, 6),
        new Vector2Int(0, 7), new Vector2Int(1, 7), new Vector2Int(2, 7), new Vector2Int(3, 7), new Vector2Int(4, 7), new Vector2Int(5, 7), new Vector2Int(6, 7), new Vector2Int(7, 7), new Vector2Int(8, 7), new Vector2Int(9, 7),
        new Vector2Int(0, 8), new Vector2Int(1, 8), new Vector2Int(2, 8), new Vector2Int(3, 8), new Vector2Int(4, 8), new Vector2Int(5, 8), new Vector2Int(6, 8), new Vector2Int(7, 8), new Vector2Int(8, 8), new Vector2Int(9, 8),
        new Vector2Int(0, 9), new Vector2Int(1, 9), new Vector2Int(2, 9), new Vector2Int(3, 9), new Vector2Int(4, 9), new Vector2Int(5, 9), new Vector2Int(6, 9), new Vector2Int(7, 9), new Vector2Int(8, 9), new Vector2Int(9, 9)
    };

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button click
        {
            Vector2Int gridPosition = GetGridPositionFromMouse();

            // Validate the grid position and attempt to spawn
            if (resourceManager.TrySpawnCharacter(gridPosition, this))
            {
                // Spawn the character and move it to the target position
                SpawnAndMoveCharacter(gridPosition, targetPosition);
            }
        }
    }

    [SerializeField]
    public List<Vector2Int> possibleTargetPositions = new List<Vector2Int>
{
    new Vector2Int(1, 1),
    new Vector2Int(3, 3),
    new Vector2Int(5, 5),
    new Vector2Int(7, 7)
};

    public void SpawnAndMoveCharacter(Vector2Int spawnPosition, Vector2Int moveToPosition)
    {
        if (spawnPosition.x < 0 || spawnPosition.y < 0) return;

        GridCell spawnCell = GetCell(spawnPosition);

        if (spawnCell != null && spawnCell.IsWalkable && spawnCell.IsCharacterSpawnable)
        {
            Vector3 worldPosition = spawnCell.CellObject.transform.position;
            GameObject newCharacter = Instantiate(CharacterPrefab, worldPosition, Quaternion.identity);

            characters.Add(newCharacter); // Store the new character in the list

            // Select a random target position from the predefined list
            if (possibleTargetPositions.Count > 0)
            {
                Vector2Int randomTarget = possibleTargetPositions[Random.Range(0, possibleTargetPositions.Count)];

                // Move the newly spawned character
                MoveCharacter(newCharacter, spawnPosition, randomTarget);
            }
            else
            {
                Debug.LogWarning("No target positions available for character to move to.");
            }
        }
        else
        {
            Debug.LogWarning($"Cell at {spawnPosition} is not spawnable for characters.");
        }
    }

    public void MoveCharacter(GameObject character, Vector2Int start, Vector2Int end)
    {
        if (pathfindingManager == null || character == null) return;

        List<Vector2Int> path = pathfindingManager.FindPath(start, end);

        CharacterCR controller = character.GetComponent<CharacterCR>();
        if (controller != null)
        {
            controller.MoveAlongPath(path, CellSize);
        }
    }

    private Vector2Int GetGridPositionFromMouse()
    {
        // Get the mouse position in world space
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0; // Ensure z-axis is zero for 2D raycasting

        // Cast a ray to detect the cell the mouse is over
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        if (hit.collider != null)
        {
            // Get the cell object hit by the raycast
            GameObject cellObject = hit.collider.gameObject;

            // Iterate through the grid to find the corresponding cell
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    if (grid[x, y].CellObject == cellObject)
                    {
                        return new Vector2Int(x, y); // Return the grid position of the clicked cell
                    }
                }
            }
        }

        // Return an invalid position if no cell was hit
        return new Vector2Int(-1, -1);
    }

    private void CreateGrid()
    {
        // Initialize the grid array with size GridSize x GridSize
        grid = new GridCell[GridSize, GridSize];

        // Loop through each grid position (x, y)
        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                // Create a Vector2Int position representing the current cell (x, y)
                Vector2Int position = new Vector2Int(x, y);

                // Create a new GridCell object with the initial settings (walkable, spawnable, default movement cost)
                GridCell cell = new GridCell(position, true, 1); // Default: walkable, spawnable for both
                grid[x, y] = cell;

                // Check if a prefab for the cell exists to instantiate visual representation
                if (CellPrefab)
                {
                    // Calculate world position for the cell
                    Vector3 worldPosition = new Vector3(x * CellSize, y * CellSize, 0);

                    // Instantiate the prefab at the calculated position
                    GameObject cellInstance = Instantiate(CellPrefab, worldPosition, Quaternion.identity);

                    // Set the instantiated cell visual object as a child of this object
                    cellInstance.transform.SetParent(transform);

                    cell.SetCellObject(cellInstance); // Link visual object to cell

                    // Set the visual feedback color based on the walkability of the cell
                    var renderer = cellInstance.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.color = cell.IsWalkable ? Color.green : Color.red;
                    }
                }
            }
        }
    }

    public List<GridCell> GetAllCells()
    {
        List<GridCell> allCells = new List<GridCell>();

        for (int x = 0; x < GridSize; x++)
        {
            for (int y = 0; y < GridSize; y++)
            {
                allCells.Add(grid[x, y]); // Add each GridCell to the list
            }
        }

        return allCells;
    }

    public List<GridCell> GetSpawnableCells()
    {
        List<GridCell> spawnableCells = new List<GridCell>();

        foreach (GridCell cell in grid)
        {
            if (cell.IsWalkable && cell.IsCharacterSpawnable)
            {
                spawnableCells.Add(cell);
                Debug.Log("Spawnable Cell Found at: " + cell.GridPosition);
            }
        }
        Debug.Log("Total Spawnable Cells: " + spawnableCells.Count);
        return spawnableCells;
    }

    public GridCell GetCell(Vector2Int position)
    {
        // Check if the position is within the bounds of the grid
        if (position.x >= 0 && position.x < GridSize && position.y >= 0 && position.y < GridSize)
        {
            return grid[position.x, position.y]; // Return the GridCell at the position
        }
        return null;
    }

    public void UpdateCell(Vector2Int position, bool isWalkable, int movementCost, bool? isCharacterSpawnable = null, bool? isEnemySpawnable = null)
    {
        // Retrieve the GridCell at the specified position
        GridCell cell = GetCell(position);
        if (cell != null)
        {
            // Update the walkable state and movement cost of the cell
            cell.SetWalkable(isWalkable);
            cell.SetMovementCost(movementCost);

            // Update spawnable properties if provided
            if (isCharacterSpawnable.HasValue)
            {
                cell.SetCharacterSpawnable(isCharacterSpawnable.Value);
            }

            if (isEnemySpawnable.HasValue)
            {
                cell.SetEnemySpawnable(isEnemySpawnable.Value);
            }

            // Update visual feedback
            if (cell.CellObject != null)
            {
                var renderer = cell.CellObject.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    if (!cell.IsWalkable)
                    {
                        renderer.color = Color.red; // Not walkable
                    }
                    else if (cell.IsWalkable && !cell.IsCharacterSpawnable)
                    {
                        renderer.color = Color.yellow; // Walkable but not spawnable for characters
                    }
                    else if (cell.IsWalkable && !cell.IsEnemySpawnable)
                    {
                        renderer.color = Color.blue; // Walkable but not spawnable for enemies
                    }
                    else
                    {
                        renderer.color = Color.green; // Walkable and spawnable for both
                    }
                }
            }
        }
    }
}
