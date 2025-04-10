using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCR : MonoBehaviour
{
    public float moveSpeed = 2f;
    private Queue<Vector3> pathPoints;
    private bool isAttacking = false;

    public LayerMask enemyLayer;

    // Event to notify the spawner when the character dies
    public event System.Action OnCharacterDeath;

    public void MoveAlongPath(List<Vector2Int> path, float cellSize)
    {
        pathPoints = new Queue<Vector3>();

        foreach (var point in path)
        {
            pathPoints.Enqueue(new Vector3(point.x * cellSize, point.y * cellSize, 0));
        }

        StartCoroutine(MoveToPoints());
    }

    private IEnumerator MoveToPoints()
    {
        while (pathPoints.Count > 0)
        {
            Vector3 targetPosition = pathPoints.Peek();

            // Move toward the targetPosition
            while (!isAttacking && Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                // REACTIVE CHECK: Support allies
                SupportAllies();

                // REACTIVE CHECK: Enemy detected — stop and fight
                if (DetectAndAttackEnemies()) // <-- Now returns a bool
                {
                    break; // Stop moving for now, handle combat
                }

                // Smooth move toward tile
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                yield return null;
            }

            // Wait until attack is done before resuming
            while (isAttacking)
            {
                yield return null;
            }

            // Only dequeue if we reached the target
            if (Vector3.Distance(transform.position, targetPosition) <= 0.01f)
            {
                pathPoints.Dequeue();
                yield return new WaitForSeconds(0.1f); // Small pause for pacing
            }
        }
    }

    private void MoveToGridPosition(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);

        float adjustedSpeed = moveSpeed;
        if (distance < 1.5f)
        {
            adjustedSpeed *= 0.6f; // Slow down when close to enemies
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, adjustedSpeed * Time.deltaTime);
    }

    private void SupportAllies()
    {
        Collider2D[] nearbyAllies = Physics2D.OverlapCircleAll(transform.position, 2f, LayerMask.GetMask("Ally"));

        foreach (Collider2D ally in nearbyAllies)
        {
            CharacterCR allyCharacter = ally.GetComponent<CharacterCR>();
            if (allyCharacter != null && allyCharacter.isAttacking)
            {
                // Help the ally by attacking the same enemy
                DetectAndAttackEnemies();
                return;
            }
        }
    }

    private IEnumerator AttackCooldownWithDodge(GameObject enemy)
    {
        yield return new WaitForSeconds(1f);

        if (enemy != null)
        {
            bool dodged = Random.value > 0.8f;

            if (dodged)
            {
                Debug.Log("Dodged the attack!");
            }
            else
            {
                // Deal damage to Enemy (projectile-shooting enemy)
                Enemy enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.TakeDamage(3);
                }

                // Deal damage to PatrolAndAttackEnemy
                PatrolAndAttackEnemy patrolEnemyScript = enemy.GetComponent<PatrolAndAttackEnemy>();
                if (patrolEnemyScript != null)
                {
                    patrolEnemyScript.TakeDamage(3);
                }
            }
        }

        isAttacking = false;
        DetectAndAttackEnemies();
    }

    private bool DetectAndAttackEnemies()
    {
        if (isAttacking) return false;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, 1f, enemyLayer);
        if (enemies.Length == 0) return false;

        Collider2D bestTarget = GetHighestPriorityEnemy(enemies);
        if (bestTarget != null)
        {
            Attack(bestTarget.gameObject);
            return true; // Enemy engaged
        }

        return false; // No target to fight
    }

    private Collider2D GetHighestPriorityEnemy(Collider2D[] enemies)
    {
        Collider2D bestTarget = null;
        float highestPriority = float.MinValue;

        foreach (Collider2D enemy in enemies)
        {
            float priority = CalculateEnemyPriority(enemy);
            if (priority > highestPriority)
            {
                highestPriority = priority;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    // Define enemy priority based on type, distance, and health
    private float CalculateEnemyPriority(Collider2D enemy)
    {
        float priority = 0;

        PatrolAndAttackEnemy patrolEnemy = enemy.GetComponent<PatrolAndAttackEnemy>();
        if (patrolEnemy != null)
        {
            priority += 10; // Give patrol enemies a higher priority
            priority -= patrolEnemy.GetCurrentHealth(); // Use the getter method
        }

        float distance = Vector3.Distance(transform.position, enemy.transform.position);
        priority -= distance * 2; // Closer enemies are prioritized more

        return priority;
    }

    private void Attack(GameObject enemy)
    {
        isAttacking = true;
        StartCoroutine(AttackCooldownWithDodge(enemy));
    }

    private IEnumerator AttackCooldown(GameObject enemy)
    {
        yield return new WaitForSeconds(1f);

        if (enemy != null)
        {
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(3);
            }

            PatrolAndAttackEnemy patrolEnemyScript = enemy.GetComponent<PatrolAndAttackEnemy>();
            if (patrolEnemyScript != null)
            {
                patrolEnemyScript.TakeDamage(3);
            }
        }

        isAttacking = false;
        DetectAndAttackEnemies();
    }

    // **NEW METHOD: Character Dies**
    public void Die()
    {
        OnCharacterDeath?.Invoke(); // Notify CharacterSpawner that this character has died
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}