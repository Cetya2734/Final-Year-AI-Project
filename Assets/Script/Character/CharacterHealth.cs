using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterHealth : MonoBehaviour
{
    public int maxHealth = 30; // Maximum health of the character
    private int currentHealth;

    public GameObject healthBarPrefab; // Assign in Inspector
    private HealthBar healthBar;

    private void Start()
    {
        currentHealth = maxHealth; // Initialize health

        // Instantiate health bar
        if (healthBarPrefab != null)
        {
            GameObject bar = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
            healthBar = bar.GetComponent<HealthBar>();
            healthBar.SetTarget(transform);
            healthBar.SetMaxHealth(maxHealth);
        }
    }

    /// Apply damage to the character.
    /// <param name="damage">The amount of damage to apply.</param>
    public void TakeDamage(int damage)
    {
        Debug.Log(gameObject.name + " took damage: " + damage);

        currentHealth -= damage; // Reduce health

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// Handle character death.
    private void Die()
    {

        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }

        Destroy(gameObject); // Remove the character from the scene
    }

    /// Heal the character by a specified amount.
    /// <param name="amount">The amount of health to restore.</param>
    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    /// Get the current health of the character.
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}
