using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GridManager gridManager;
    public float checkInterval = 1f;  // How often to check for enemies
    private float nextCheckTime = 0f;

    private void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;

            // Check for enemies once and store the result
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            // If there are no enemies left, regenerate the grid
            if (enemies.Length == 0)
            {
                Debug.Log("All enemies defeated. Regenerating grid...");
                gridManager.GenerateNewLevelLayout();  // Call your grid regeneration method
            }
        }
    }
}
