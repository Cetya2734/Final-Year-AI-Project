using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFindingManager : MonoBehaviour
{
    public GridManager gridManager;

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        // Initialize a queue for BFS frontier and a dictionary to track the path
        Queue<Vector2Int> frontier = new Queue<Vector2Int>(); // Queue holds the positions to explore
        Dictionary<Vector2Int, Vector2Int?> cameFrom = new Dictionary<Vector2Int, Vector2Int?>(); // Keeps track of where each position came from

        // Start the search from the start position
        frontier.Enqueue(start);
        cameFrom[start] = null;

        // Continue exploring until there are no more positions to explore
        while (frontier.Count > 0)
        {
            // Dequeue the next position to explore
            Vector2Int current = frontier.Dequeue();

            // If the current position is the end position, stop the search
            if (current == end)
                break;

            // Check all the neighbors of the current position
            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                // If the neighbor hasn't been visited and is walkable, add it to the frontier
                if (!cameFrom.ContainsKey(neighbor) && gridManager.GetCell(neighbor)?.IsWalkable == true)
                {
                    frontier.Enqueue(neighbor); // Add to the queue for exploration
                    cameFrom[neighbor] = current; // Record that we came to this neighbor from the current position
                }
            }
        }

        // Reconstruct path
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int? step = end;

        // Follow the path from end to start and add each step to the path list
        while (step != null)
        {
            path.Add(step.Value); // Add the current step to the path
            step = cameFrom[step.Value]; // Move to the previous step in the path
        }

        // Reverse the path to make it from start to end
        path.Reverse();
        return path;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int position)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>
        {
            position + Vector2Int.up, // Up neighbor
            position + Vector2Int.down, // Down neighbor
            position + Vector2Int.left, // Left neighbor
            position + Vector2Int.right // Right neighbor
        };

        return neighbors; // Return the list of neighbors
    }
}
