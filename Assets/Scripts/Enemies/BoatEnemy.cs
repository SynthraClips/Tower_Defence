using UnityEngine;

public enum BoatTier { Skiff = 1, Cutter = 2, Frigate = 3 }

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BoatEnemy : Enemy
{
    [Header("Boat Tier (sets stats on spawn)")]
    public BoatTier tier = BoatTier.Skiff;

    protected override void Awake()
    {
        base.Awake();
        type = EnemyType.Water;

        // Ensure 2D physics are set sensibly
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void SetTier(BoatTier t)
    {
        tier = t;
        // Configure stats per tier:
        switch (tier)
        {
            case BoatTier.Skiff:
                maxHealth   = 20;
                moveSpeed   = 1.8f;
                damageToBase= 1;
                goldReward  = 5;
                break;
            case BoatTier.Cutter:
                maxHealth   = 60;
                moveSpeed   = 1.4f;
                damageToBase= 2;
                goldReward  = 10;
                break;
            case BoatTier.Frigate:
                maxHealth   = 150;
                moveSpeed   = 1.1f;
                damageToBase= 3;
                goldReward  = 20;
                break;
        }
        // Reset runtime state for fresh spawn:
        currentHealth = maxHealth;
    }
}
