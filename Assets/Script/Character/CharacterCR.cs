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

            while (!isAttacking && Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                DetectAndAttackEnemies();
                yield return null;
            }

            while (isAttacking)
            {
                yield return null;
            }

            if (Vector3.Distance(transform.position, targetPosition) <= 0.01f)
            {
                pathPoints.Dequeue();
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
            PatrolAndAttackEnemy patrolEnemyScript = enemy.GetComponent<PatrolAndAttackEnemy>();
            if (patrolEnemyScript != null)
            {
                // 20% chance to dodge instead of taking damage
                if (Random.value > 0.8f)
                {
                    Debug.Log("Dodged the attack!");
                }
                else
                {
                    patrolEnemyScript.TakeDamage(3);
                }
            }
        }

        isAttacking = false;
        DetectAndAttackEnemies();
    }

    private void DetectAndAttackEnemies()
    {
        if (isAttacking) return;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, 1f, enemyLayer);

        if (enemies.Length == 0) return;

        // Pick the best target instead of the closest one
        Collider2D bestTarget = GetHighestPriorityEnemy(enemies);

        if (bestTarget != null)
        {
            Attack(bestTarget.gameObject);
        }
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