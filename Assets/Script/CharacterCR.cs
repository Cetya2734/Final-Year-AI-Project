using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCR : MonoBehaviour
{
    public float moveSpeed = 2f; // Movement speed in units per second
    private Queue<Vector3> pathPoints;
    private bool isAttacking = false;

    public LayerMask enemyLayer; // Layer mask to detect enemies

    public void MoveAlongPath(List<Vector2Int> path, float cellSize)
    {
        pathPoints = new Queue<Vector3>();

        // Convert grid positions to world positions
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
            Vector3 targetPosition = pathPoints.Peek(); // Look at the next point without dequeuing

            // Move to the target position if not attacking
            while (!isAttacking && Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                // Check for nearby enemies
                DetectAndAttackEnemies();

                yield return null;
            }

            // If attacking, wait until attack is finished
            while (isAttacking)
            {
                yield return null;
            }

            // If we reach the target position, dequeue it
            if (Vector3.Distance(transform.position, targetPosition) <= 0.01f)
            {
                pathPoints.Dequeue();
            }
        }
    }

    private void DetectAndAttackEnemies()
    {
        if (isAttacking) return;

        // Detect enemies in the vicinity using a radius around the character's current position
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, 1f, enemyLayer); // 1f is the detection radius

        foreach (Collider2D enemy in enemies)
        {
            // Attack the first valid enemy found
            if (enemy != null)
            {
                Attack(enemy.gameObject);
                break; // Stop further detection once an attack begins
            }
        }
    }

    private void Attack(GameObject enemy)
    {
        isAttacking = true;

        // Simulate attack delay (e.g., attack duration)
        StartCoroutine(AttackCooldown(enemy));
    }

    private IEnumerator AttackCooldown(GameObject enemy)
    {
        // Simulate attack time
        yield return new WaitForSeconds(1f);

        if (enemy != null)
        {
            // Check if the enemy is a general Enemy
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(5); // Adjust damage value as needed
            }

            // Check if the enemy is a PatrolAndAttackEnemy
            PatrolAndAttackEnemy patrolEnemyScript = enemy.GetComponent<PatrolAndAttackEnemy>();
            if (patrolEnemyScript != null)
            {
                patrolEnemyScript.TakeDamage(5); // Adjust damage value as needed
            }
        }

        // Resume movement and allow detecting new enemies
        isAttacking = false;

        // Check for other nearby enemies after the current attack
        DetectAndAttackEnemies();
    }

    // Optional: Visualize the detection radius in the scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f); // Match the detection radius
    }
}