using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private int damage; // Damage dealt by the projectile

    public void SetDamage(int value)
    {
        damage = value;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) // Ensure it collides with the player
        {
            CharacterHealth characterHealth = collision.GetComponent<CharacterHealth>();
            if (characterHealth != null)
            {
                characterHealth.TakeDamage(damage);
            }

            Destroy(gameObject); // Destroy the projectile after hitting the player
        }
    }
}
