using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCR : MonoBehaviour
{
    public float moveSpeed = 2f;
    private Queue<Vector3> pathPoints;
    private bool isAttacking = false;

    public LayerMask enemyLayer;
    public GridManager gridManager; // Assign this in inspector or via script
    public float cellSize = 1f; // Should match your GridManager cell size

    // Event to notify the spawner when the character dies
    public event System.Action OnCharacterDeath;

    public void MoveAlongPath(List<Vector2Int> path, float cellSize)
    {
        pathPoints = new Queue<Vector3>();

        foreach (var point in path)
        {
            pathPoints.Enqueue(new Vector3(point.x * cellSize, point.y * cellSize, 0));
        }

        StopAllCoroutines(); // Stop previous movement if any
        StartCoroutine(MoveToPoints());
    }

    private IEnumerator MoveToPoints()
    {
        while (pathPoints.Count > 0)
        {
            Vector3 targetPosition = pathPoints.Peek();

            while (!isAttacking && Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                SupportAllies();

                if (DetectAndAttackEnemies())
                    break;

                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                yield return null;
            }

            while (isAttacking)
            {
                yield return null;
            }

            if (Vector3.Distance(transform.position, targetPosition) <= 0.01f)
            {
                pathPoints.Dequeue();
                yield return new WaitForSeconds(0.1f);
            }
        }

        // When done moving, re-evaluate next action
        yield return new WaitForSeconds(0.5f); // Optional small delay
        //DecideNextAction();
    }

    private void MoveToGridPosition(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);

        float adjustedSpeed = moveSpeed;
        if (distance < 1.5f)
        {
            adjustedSpeed *= 0.6f;
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
                DetectAndAttackEnemies();
                return;
            }
        }
    }

    private IEnumerator AttackCooldownWithDodge(GameObject enemy)
    {
        yield return new WaitForSeconds(1f);  // Cooldown between attacks

        if (enemy != null)
        {
            bool dodged = Random.value > 0.8f;  // Simple dodge logic, could be improved with more factors

            if (dodged)
            {
                Debug.Log("Dodged the attack!");
            }
            else
            {
                Enemy enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.TakeDamage(5);
                }

                PatrolAndAttackEnemy patrolEnemyScript = enemy.GetComponent<PatrolAndAttackEnemy>();
                if (patrolEnemyScript != null)
                {
                    patrolEnemyScript.TakeDamage(5);
                }
            }
        }

        isAttacking = false;  // Reset attack state
        DetectAndAttackEnemies();  // Check for more potential targets
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
            return true;
        }

        return false;
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

    private float CalculateEnemyPriority(Collider2D enemy)
    {
        float priority = 0;

        PatrolAndAttackEnemy patrolEnemy = enemy.GetComponent<PatrolAndAttackEnemy>();
        if (patrolEnemy != null)
        {
            priority += 10;
            priority -= patrolEnemy.GetCurrentHealth();
        }

        float distance = Vector3.Distance(transform.position, enemy.transform.position);
        priority -= distance * 2;

        return priority;
    }

    private GameObject GetClosestEnemyInScene()
    {
        Collider2D[] allEnemies = Physics2D.OverlapCircleAll(transform.position, 50f, enemyLayer);
        Collider2D bestTarget = GetHighestPriorityEnemy(allEnemies);
        return bestTarget ? bestTarget.gameObject : null;
    }

    private void Attack(GameObject enemy)
    {
        isAttacking = true;
        StartCoroutine(AttackCooldownWithDodge(enemy));
    }

    public void Die()
    {
        OnCharacterDeath?.Invoke();
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}