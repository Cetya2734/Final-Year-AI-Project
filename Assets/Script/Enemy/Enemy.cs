using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 10;
    private int currentHealth;

    [Header("Combat")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float shootingInterval = 2f;
    public float projectileSpeed = 5f;
    public int projectileDamage = 5;
    public float detectionRange = 5f;

    [Header("Health UI")]
    public GameObject healthBarPrefab; // Assign a prefab with HealthBar script
    private HealthBar healthBar;

    private Transform targetCharacter;
    private float lastShotTime = 0f;

    private void Start()
    {
        currentHealth = maxHealth;

        // Instantiate and setup the health bar
        if (healthBarPrefab != null)
        {
            GameObject bar = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
            healthBar = bar.GetComponent<HealthBar>();
            healthBar.SetTarget(transform);
            healthBar.SetMaxHealth(maxHealth);
        }
    }

    private void Update()
    {
        FindClosestPlayer();

        if (targetCharacter != null && Time.time - lastShotTime > shootingInterval)
        {
            ShootAtCharacter();
            lastShotTime = Time.time;
        }
    }

    private void FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closestPlayer = null;
        float closestDistance = detectionRange;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= closestDistance)
            {
                closestPlayer = player.transform;
                closestDistance = distance;
            }
        }

        targetCharacter = closestPlayer;
    }

    private void ShootAtCharacter()
    {
        if (projectilePrefab != null && firePoint != null && targetCharacter != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Vector3 direction = (targetCharacter.position - firePoint.position).normalized;

            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * projectileSpeed;
            }

            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.SetDamage(projectileDamage);
            }

            Destroy(projectile, 5f);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }

        Destroy(gameObject);
    }
}