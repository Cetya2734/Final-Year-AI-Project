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

    private float noticeDelay = 1f;
    private float noticeStartTime;

    private float lostPlayerTimeout = 3f;
    private float lastSeenTime;

    private float patrolPauseTime = 1f;
    private float waitStartTime;
    private bool waitingAtPatrolPoint = false;

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
        if (patrolPoints.Count == 0) return;

        Vector2Int targetPatrolPoint = patrolPoints[currentPatrolIndex];
        Vector3 targetPos = gridManager.GetCell(targetPatrolPoint).CellObject.transform.position;

        if (waitingAtPatrolPoint)
        {
            if (Time.time - waitStartTime >= patrolPauseTime)
            {
                waitingAtPatrolPoint = false;

                int newIndex;
                do
                {
                    newIndex = Random.Range(0, patrolPoints.Count);
                } while (newIndex == currentPatrolIndex && patrolPoints.Count > 1);

                currentPatrolIndex = newIndex;
            }
            return;
        }

        MoveToGridPosition(targetPatrolPoint);

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            waitingAtPatrolPoint = true;
            waitStartTime = Time.time;
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
        if (noticeStartTime == 0f)
            noticeStartTime = Time.time;

        if (Time.time - noticeStartTime >= noticeDelay)
        {
            noticeStartTime = 0f;
            SwitchState(EnemyState.Chasing);
        }
    }

    private void ChasePlayer()
    {
        if (targetCharacter == null)
        {
            SwitchState(EnemyState.Patrolling);
            return;
        }

        float distance = Vector3.Distance(transform.position, targetCharacter.position);
        if (distance > detectionRange)
        {
            if (Time.time - lastSeenTime > lostPlayerTimeout)
            {
                targetCharacter = null;
                SwitchState(EnemyState.Patrolling);
                return;
            }
        }
        else
        {
            lastSeenTime = Time.time;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetCharacter.position, movementSpeed * Time.deltaTime);

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
        
        Destroy(gameObject);
    }

    private void SwitchState(EnemyState newState)
    {
        currentState = newState;
    }
}
