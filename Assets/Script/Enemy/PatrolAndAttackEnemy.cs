using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolAndAttackEnemy : MonoBehaviour
{
    public GameObject healthBarPrefab; // Assign this in the Inspector
    private HealthBar healthBar;

    public int maxHealth = 20;

    private float lostTargetCheckDuration = 1f;
    private float lostTargetCheckStartTime = -1f;

    [SerializeField]
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

    private float attackBufferTime = 0.2f;

    private float dodgeCooldown = 2f; // Cooldown time for dodge
    private float lastDodgeTime = 0f;

    private enum EnemyState { Patrolling, Noticing, Chasing, Attacking, Searching }
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

        // Create and setup the health bar
        if (healthBarPrefab != null)
        {
            GameObject bar = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
            healthBar = bar.GetComponent<HealthBar>();
            healthBar.SetTarget(transform);        // Makes it follow the enemy
            healthBar.SetMaxHealth(maxHealth);     // Set full HP
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrolling:
                HandlePatrolling();
                break;

            case EnemyState.Noticing:
                HandleNoticing();
                break;

            case EnemyState.Chasing:
                HandleChasing();
                break;

            case EnemyState.Attacking:
                HandleAttacking();
                break;

            case EnemyState.Searching:
                HandleSearching();
                break;
        }
    }

    private void HandlePatrolling()
    {
        Patrol();
        DetectPlayer();
    }

    private void HandleNoticing()
    {
        NoticePlayer();
    }

    private void HandleChasing()
    {
        ChasePlayer();
    }
    private void HandleAttacking()
    {
        AttackPlayer();
    }
    private void HandleSearching()
    {
        SearchForPlayer();
    }

    private void Patrol()
    {
        if (patrolPoints.Count == 0) return;

        if (waitingAtPatrolPoint)
        {
            if (Time.time - waitStartTime >= patrolPauseTime)
            {
                waitingAtPatrolPoint = false;

                // Weighted patrol behavior (favor recently seen areas)
                int newIndex;
                do
                {
                    newIndex = Random.Range(0, patrolPoints.Count);
                } while (newIndex == currentPatrolIndex && patrolPoints.Count > 1);

                currentPatrolIndex = newIndex;
            }
            return;
        }

        Vector2Int targetPatrolPoint = patrolPoints[currentPatrolIndex];
        MoveToGridPosition(targetPatrolPoint);

        // Stop at patrol point before moving to the next one
        if (Vector3.Distance(transform.position, gridManager.GetCell(targetPatrolPoint).CellObject.transform.position) < 0.1f)
        {
            waitingAtPatrolPoint = true;
            waitStartTime = Time.time;
        }
    }

    private void DetectPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closestPlayer = null;
        float closestDistance = detectionRange;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            // Raycast from enemy to player to check for obstacles
            RaycastHit2D hit = Physics2D.Raycast(transform.position, (player.transform.position - transform.position).normalized, detectionRange);

            // If the raycast hits the player (and not a wall)
            if (hit.collider != null && hit.collider.CompareTag("Player") && distance <= closestDistance)
            {
                closestPlayer = player.transform;
                closestDistance = distance;
            }
        }

        if (closestPlayer != null)
        {
            targetCharacter = closestPlayer;
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
                Debug.Log("Enemy lost player, searching...");
                SwitchState(EnemyState.Searching);
                return;
            }
        }
        else
        {
            lastSeenTime = Time.time;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetCharacter.position, movementSpeed * Time.deltaTime);

        if (distance <= attackRange + attackBufferTime)
        {
            SwitchState(EnemyState.Attacking);
        }
    }

    private void SearchForPlayer()
    {
        if (Time.time - lastSeenTime > lostPlayerTimeout)
        {
            Debug.Log("Enemy gave up searching, returning to patrol...");
            SwitchState(EnemyState.Patrolling);
        }
        else
        {
            // Move randomly within a small radius of the last seen position
            Vector2 randomSearchPosition = (Vector2)transform.position + Random.insideUnitCircle * 2f;
            transform.position = Vector3.MoveTowards(transform.position, randomSearchPosition, movementSpeed * Time.deltaTime);
        }
    }

    private void AttackPlayer()
    {
        if (targetCharacter == null || !targetCharacter.gameObject.activeInHierarchy)
        {
            // Start or continue a short "look around" delay before giving up
            if (lostTargetCheckStartTime < 0f)
                lostTargetCheckStartTime = Time.time;

            if (Time.time - lostTargetCheckStartTime >= lostTargetCheckDuration)
            {
                //Debug.LogWarning("Target is gone. Returning to Patrolling.");
                lostTargetCheckStartTime = -1f; // Reset for next time
                SwitchState(EnemyState.Patrolling);
            }

            return;
        }

        // Reset the lost target timer if target is valid again
        lostTargetCheckStartTime = -1f;

        float distance = Vector3.Distance(transform.position, targetCharacter.position);
        //Debug.Log($"{gameObject.name} is trying to attack {targetCharacter.name} at distance {distance}");

        if (distance > attackRange)
        {
            SwitchState(EnemyState.Chasing);
            return;
        }

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            CharacterHealth characterHealth = targetCharacter.GetComponent<CharacterHealth>();

            if (characterHealth != null)
            {
                characterHealth.TakeDamage(attackDamage);
                //Debug.Log($"Enemy dealt {attackDamage} damage to {targetCharacter.name}");
                lastAttackTime = Time.time;
            }
            else
            {
                Debug.LogWarning($"{targetCharacter.name} has no CharacterHealth!");
            }
        }
    }

    // Second attack function for combos
    private void SecondaryAttack()
    {
        if (targetCharacter != null && Vector3.Distance(transform.position, targetCharacter.position) <= attackRange)
        {
            CharacterHealth characterHealth = targetCharacter.GetComponent<CharacterHealth>();
            if (characterHealth != null)
            {
                characterHealth.TakeDamage(attackDamage / 2); // Weaker second attack
            }
        }
    }

    private void Dodge()
    {
        if (Time.time - lastDodgeTime < dodgeCooldown)
            return;

        float dodgeChance = 0.3f; // Default dodge chance
        if (currentHealth <= maxHealth / 3) dodgeChance = 0.5f;

        if (Random.value < dodgeChance)
        {
            Vector2 dodgeDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            transform.position += (Vector3)dodgeDirection * 1.5f;
            lastDodgeTime = Time.time; // Reset dodge timer
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

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        if (Random.value < 0.1f)
        {
            Dodge();
        }

        if (currentHealth <= maxHealth / 3)
        {
            EnterRetreatMode();
        }

        if (currentHealth <= maxHealth / 2)
        {
            EnterEnrageMode();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    private void EnterRetreatMode()
    {
        Debug.Log("Enemy is retreating!");
        SwitchState(EnemyState.Patrolling); // Retreat to a random patrol point
    }

    private void EnterEnrageMode()
    {
        Debug.Log("Enemy is enraged!");
        attackDamage *= 2;  // Double damage
        movementSpeed *= 1.1f;  // Increase speed slightly

        // Visual change to indicate rage
        GetComponent<SpriteRenderer>().color = Color.red;  // Change to red (example)
                                                           // Optionally, you can trigger an animation here as well.

        // Play enraged sound effect (if you have a sound manager)
        //AudioManager.Instance.PlaySound("EnrageSound");
    }

    private void Die()
    {
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }

        Destroy(gameObject);
    }

    private void SwitchState(EnemyState newState)
    {
        currentState = newState;
    }
}
