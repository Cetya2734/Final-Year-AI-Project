using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridCell
{
    public Vector2Int Position;     // Grid coordinates (x, y)
    public bool IsWalkable;         // Can a character walk on this cell?
    public int MovementCost;        // Cost of moving through this cell
    public bool IsCharacterSpawnable; // Is this cell spawnable for characters?
    public bool IsEnemySpawnable;     // Is this cell spawnable for enemies?
    public GameObject CellObject;   // Reference to the visual representation
    public Vector2Int GridPosition { get; private set; }  // Store the grid position

    public GridCell(Vector2Int position, bool isWalkable, int movementCost, bool isCharacterSpawnable = true, bool isEnemySpawnable = true)
    {
        GridPosition = position;  // Store position
        Position = position;
        IsWalkable = isWalkable;
        MovementCost = movementCost;
        IsCharacterSpawnable = isCharacterSpawnable; // Default: character can spawn
        IsEnemySpawnable = isEnemySpawnable;         // Default: enemy can spawn
        CellObject = null;
    }

    public Vector3 GetWorldPosition()
    {
        return new Vector3(GridPosition.x, GridPosition.y, 0);
    }

    //Methods to modify cell properties
    public void SetWalkable(bool walkable)
    {
        IsWalkable = walkable;
    }

    public void SetMovementCost(int cost)
    {
        MovementCost = cost;
    }

    public void SetCharacterSpawnable(bool spawnable)
    {
        IsCharacterSpawnable = spawnable;
    }

    public void SetEnemySpawnable(bool spawnable)
    {
        IsEnemySpawnable = spawnable;
    }

    public void SetCellObject(GameObject cellObject)
    {
        CellObject = cellObject;
    }
}
