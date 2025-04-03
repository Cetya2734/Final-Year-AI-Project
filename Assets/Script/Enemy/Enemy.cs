using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 10; // Maximum health of the enemy
    private int currentHealth;

    public GameObject projectilePrefab; // The projectile to shoot
    public Transform firePoint; // Point where projectiles are spawned
    public float shootingInterval = 2f; // Time between shots
    public float projectileSpeed = 5f; // Speed of the projectile
    public int projectileDamage = 5; // Damage dealt by the projectile
    public float detectionRange = 5f; // Range at which enemy detects the player

    private Transform targetCharacter; // Reference to the character
    private float lastShotTime = 0f; // Time of the last shot

    private void Start()
    {
        currentHealth = maxHealth; // Initialize health
    }

    private void Update()
    {
        // Continuously update the target to be the closest player within detection range
        FindClosestPlayer();

        // If a target is found, shoot at it
        if (targetCharacter != null && Time.time - lastShotTime > shootingInterval)
        {
            ShootAtCharacter();
            lastShotTime = Time.time; // Update the time of the last shot
        }
    }

    private void FindClosestPlayer()
    {
        // Find all game objects tagged with "Player"
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform closestPlayer = null; // To store the closest player
        float closestDistance = detectionRange; // Start with the maximum detection range

        // Loop through all players to find the closest one
        foreach (GameObject player in players)
        {
            // Calculate the distance from the current position to the player's position
            float distance = Vector3.Distance(transform.position, player.transform.position);

            // Check if player is within detection range and closer than the current closest player
            if (distance <= closestDistance)
            {
                closestPlayer = player.transform; // Set the closest player
                closestDistance = distance; // Update the closest distance
            }
        }

        // If a player is found within range, set as target
        if (closestPlayer != null)
        {
            targetCharacter = closestPlayer; // Set target character to closest player
        }
        else
        {
            targetCharacter = null; // No players within detection range
        }
    }

    private void ShootAtCharacter()
    {
        // Check if a projectile prefab and fire point exist, and target character is set
        if (projectilePrefab != null && firePoint != null && targetCharacter != null)
        {
            // Instantiate projectile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

            // Set direction and speed
            Vector3 direction = (targetCharacter.position - firePoint.position).normalized;
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * projectileSpeed; // Apply velocity in the direction of the target
            }

            // Attach damage info to the projectile
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.SetDamage(projectileDamage); // Set the projectile's damage
            }

            Destroy(projectile, 5f); // Destroy after 5 seconds if it doesn't hit anything
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage; // Reduce health

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// Handle enemy death.
    private void Die()
    {
        Destroy(gameObject); // Remove the enemy from the scene
    }
}
