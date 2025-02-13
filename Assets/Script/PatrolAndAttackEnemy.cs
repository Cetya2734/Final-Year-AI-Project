using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolAndAttackEnemy : MonoBehaviour
{
    public int maxHealth = 20;
    private int currentHealth;

    public float detectionRange = 3f; // Detection range
    public float attackRange = 1f;    // Melee attack range
    public int attackDamage = 10;     // Attack damage
    public float attackCooldown = 1.5f; // Cooldown between attacks
    public float movementSpeed = 2f;  // Movement speed

    public List<Vector2Int> patrolPoints; // Patrol waypoints
    private int currentPatrolIndex = 0;

    private Transform targetCharacter; // The current detected player
    private float lastAttackTime = 0f;

    private enum EnemyState { Patrolling, Noticing, Chasing, Attacking }
    private EnemyState currentState = EnemyState.Patrolling;

    private GridManager gridManager;

    private void Start()
    {
        currentHealth = maxHealth;
        gridManager = FindObjectOfType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError("GridManager not found in the scene!");
        }
    }

    private void Update()
    {
        // Debugging state
        Debug.Log($"Current State: {currentState}");

        switch (currentState)
        {
            case EnemyState.Patrolling:
                Patrol();
                DetectPlayer();
                break;

            case EnemyState.Noticing:
                NoticePlayer();
                break;

            case EnemyState.Chasing:
                ChasePlayer();
                break;

            case EnemyState.Attacking:
                AttackPlayer();
                break;
        }
    }

    private void Patrol()
    {
        // If there are no patrol points defined, exit the method
        if (patrolPoints.Count == 0) return;

        // Get the current target patrol point based on the current index
        Vector2Int targetPatrolPoint = patrolPoints[currentPatrolIndex];

        // Move towards the target patrol point on the grid
        MoveToGridPosition(targetPatrolPoint);

        // Check if the enemy is close enough to the target patrol point
        if (Vector2.Distance(transform.position, gridManager.GetCell(targetPatrolPoint).CellObject.transform.position) < 0.1f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
        }
    }

    private void DetectPlayer()
    {
        // Find all game objects tagged as "Player" in the scene
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        // Placeholder for the closest player's transform and their distance
        Transform closestPlayer = null;
        float closestDistance = detectionRange; // Start with the maximum detection range

        // Iterate through all players to find the closest one within the detection range
        foreach (GameObject player in players)
        {
            // Calculate the distance between this enemy and the player
            float distance = Vector3.Distance(transform.position, player.transform.position);

            // If the distance is smaller than the current closest distance, update the closest player
            if (distance <= closestDistance)
            {
                closestPlayer = player.transform;
                closestDistance = distance;
            }
        }

        // If a player is found within the detection range
        if (closestPlayer != null)
        {
            // Set the closest player as the target character
            targetCharacter = closestPlayer;

            // Switch the enemy's state to "Noticing" to trigger appropriate behavior
            SwitchState(EnemyState.Noticing);
        }
    }

    private void NoticePlayer()
    {
        // Simulate noticing the player (could add animations or a delay here)
        Debug.Log("Enemy has noticed the player!");
        SwitchState(EnemyState.Chasing);
    }

    private void ChasePlayer()
    {
        // If the target player is lost (null), switch back to patrolling
        if (targetCharacter == null)
        {
            SwitchState(EnemyState.Patrolling);
            return;
        }

        // Move towards the player's position at a speed determined by movementSpeed
        transform.position = Vector3.MoveTowards(transform.position, targetCharacter.position, movementSpeed * Time.deltaTime);

        // Calculate the distance to the player
        float distance = Vector3.Distance(transform.position, targetCharacter.position);

        // If the player is within attack range, switch to the Attacking state
        if (distance <= attackRange)
        {
            SwitchState(EnemyState.Attacking);
        }
    }

    private void AttackPlayer()
    {
        // If the target player is lost or moves out of attack range, switch back to chasing
        if (targetCharacter == null || Vector3.Distance(transform.position, targetCharacter.position) > attackRange)
        {
            SwitchState(EnemyState.Chasing);
            return;
        }

        // Check if enough time has passed since the last attack
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            // Get the health component of the player to deal damage
            CharacterHealth characterHealth = targetCharacter.GetComponent<CharacterHealth>();
            if (characterHealth != null)
            {
                // Apply damage to the player's health
                characterHealth.TakeDamage(attackDamage);

                // Record the time of this attack to manage cooldown
                lastAttackTime = Time.time;
                Debug.Log($"Attacked player for {attackDamage} damage.");
            }
        }
    }

    private void MoveToGridPosition(Vector2Int gridPosition)
    {
        // Get the target grid cell from the grid manager
        GridCell targetCell = gridManager.GetCell(gridPosition);

        // Ensure the target cell exists and is walkable
        if (targetCell != null && targetCell.IsWalkable)
        {
            // Get the world position of the target cell
            Vector3 targetWorldPosition = targetCell.CellObject.transform.position;

            // Move towards the target cell's world position at a speed determined by movementSpeed
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, movementSpeed * Time.deltaTime);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy has died.");
        Destroy(gameObject);
    }

    private void SwitchState(EnemyState newState)
    {
        currentState = newState;
    }
}
