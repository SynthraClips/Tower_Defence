using UnityEngine;

public enum BoatTier { Weak = 1, Medium = 2, Hard = 3, Swift = 4 }

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BoatEnemy : Enemy
{
    [Header("Boat Tier (legacy support)")]
    public BoatTier tier = BoatTier.Weak;
    [SerializeField] private SpriteRenderer spriteRenderer;
    private BoatEnemyDefinition activeDefinition;

    protected override void Awake()
    {
        base.Awake();
        type = EnemyType.Water;

        // Ensure 2D physics are set sensibly
        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (!spriteRenderer)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public void SetTier(BoatTier t)
    {
        tier = t;
        // Configure stats per tier:
        switch (tier)
        {
            case BoatTier.Weak:
                ApplyLegacyValues(18, 1.7f, 1, 5, new Color(0.78f, 0.93f, 1f), new Vector3(0.9f, 0.9f, 1f));
                break;
            case BoatTier.Medium:
                ApplyLegacyValues(45, 1.3f, 1, 9, new Color(0.97f, 0.9f, 0.55f), new Vector3(1f, 1f, 1f));
                break;
            case BoatTier.Hard:
                ApplyLegacyValues(90, 1.0f, 2, 16, new Color(0.98f, 0.67f, 0.42f), new Vector3(1.12f, 1.12f, 1f));
                break;
            case BoatTier.Swift:
                ApplyLegacyValues(12, 2.2f, 1, 4, new Color(0.7f, 1f, 0.78f), new Vector3(0.78f, 0.78f, 1f));
                break;
        }
    }

    public void ApplyDefinition(BoatEnemyDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        activeDefinition = definition;
        name = definition.displayName;
        maxHealth = Mathf.Max(1, definition.maxHealth);
        moveSpeed = Mathf.Max(0.01f, definition.moveSpeed);
        damageToBase = Mathf.Max(1, definition.damageToBase);
        goldReward = Mathf.Max(0, definition.goldReward);
        currentHealth = maxHealth;

        if (spriteRenderer)
        {
            spriteRenderer.color = definition.tint;
        }

        transform.localScale = definition.scale;
    }

    protected override int ResolveIncomingDamage(int incomingDamage, DamageType damageType)
    {
        float multiplier = 1f;
        if (activeDefinition != null)
        {
            multiplier = damageType switch
            {
                DamageType.Siege => activeDefinition.siegeDamageMultiplier,
                DamageType.Arcane => activeDefinition.arcaneDamageMultiplier,
                DamageType.Airburst => activeDefinition.airburstDamageMultiplier,
                _ => 1f,
            };

            incomingDamage = Mathf.Max(0, incomingDamage - activeDefinition.flatArmor);
        }

        return Mathf.Max(0, Mathf.RoundToInt(incomingDamage * multiplier));
    }

    private void ApplyLegacyValues(int health, float speed, int baseDamage, int reward, Color tint, Vector3 scale)
    {
        maxHealth = health;
        moveSpeed = speed;
        damageToBase = baseDamage;
        goldReward = reward;
        currentHealth = maxHealth;

        if (spriteRenderer)
        {
            spriteRenderer.color = tint;
        }

        transform.localScale = scale;
    }
}
