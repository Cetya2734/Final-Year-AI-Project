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

    private void DetectAndAttackEnemies()
    {
        if (isAttacking) return;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, 1f, enemyLayer);

        if (enemies.Length == 0) return;

        if (enemies.Length == 1)
        {
            Attack(enemies[0].gameObject);
            return;
        }

        Collider2D closestEnemy = null;
        float minDistanceSqr = float.MaxValue;

        foreach (Collider2D enemy in enemies)
        {
            float distanceSqr = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
        {
            Attack(closestEnemy.gameObject);
        }
    }

    private void Attack(GameObject enemy)
    {
        isAttacking = true;
        StartCoroutine(AttackCooldown(enemy));
    }

    private IEnumerator AttackCooldown(GameObject enemy)
    {
        yield return new WaitForSeconds(1f);

        if (enemy != null)
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