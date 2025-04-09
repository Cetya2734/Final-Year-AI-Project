using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolAndAttackEnemy : MonoBehaviour
{
    public int maxHealth = 20;

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
    }

    private void Update()
    {

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

            case EnemyState.Searching:
                SearchForPlayer();
                break;
        }
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

        if (distance <= attackRange)
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
        if (targetCharacter == null)
        {
            Debug.LogWarning("Target is null. Switching to Patrolling.");
            SwitchState(EnemyState.Patrolling);
            return;
        }

        if (!targetCharacter.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Target is destroyed or disabled. Switching to Patrolling.");
            SwitchState(EnemyState.Patrolling);
            return;
        }

        float distance = Vector3.Distance(transform.position, targetCharacter.position);
        Debug.Log($"{gameObject.name} is trying to attack {targetCharacter.name} at distance {distance}");

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
                Debug.Log($"Enemy dealt {attackDamage} damage to {targetCharacter.name}");

                if (Random.value < 0.5f)
                    Invoke(nameof(SecondaryAttack), 0.5f);

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
        if (Random.value < 0.3f) // 30% chance to dodge
        {
            Vector2 dodgeDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            transform.position += (Vector3)dodgeDirection * 1.5f; // Moves enemy away
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

        if (currentHealth <= maxHealth / 3) // If HP is below 33%
        {
            EnterRetreatMode();
        }

        if (currentHealth <= maxHealth / 2) // If HP is below 50%, enter enrage mode
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
        attackDamage *= 2;
        movementSpeed *= 1.2f;
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
