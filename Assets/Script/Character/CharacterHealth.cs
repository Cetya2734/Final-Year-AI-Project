using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterHealth : MonoBehaviour
{
    public int maxHealth = 30; // Maximum health of the character
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth; // Initialize health
    }

    /// Apply damage to the character.
    /// <param name="damage">The amount of damage to apply.</param>
    public void TakeDamage(int damage)
    {
        currentHealth -= damage; // Reduce health
        

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// Handle character death.
    private void Die()
    {
        
        Destroy(gameObject); // Remove the character from the scene
    }

    /// Heal the character by a specified amount.
    /// <param name="amount">The amount of health to restore.</param>
    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth; // Ensure health doesn't exceed maxHealth
        }

        
    }

    /// Get the current health of the character.
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}
