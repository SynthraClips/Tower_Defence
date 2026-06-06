using UnityEngine;

public enum EnemyType { Water, Air }

public abstract class Enemy : MonoBehaviour
{
    public Spawner ownerSpawner;

    [Header("Stats")]
    public EnemyType type = EnemyType.Water;
    public float moveSpeed = 1.5f;
    public int maxHealth = 10;
    public int damageToBase = 1;
    public int goldReward = 5;
    public void ForceSetCurrentHealth(int hp) { currentHealth = Mathf.Max(1, hp); }

    [HideInInspector] public Path path;

    protected int currentHealth;
    protected int waypointIndex;
    protected Transform targetWaypoint;

    public int CurrentHealth => currentHealth;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    protected virtual void Start()
    {
        waypointIndex = 0;
        targetWaypoint = path?.GetWaypoint(waypointIndex);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[Enemy] {name} spawned without a valid water path.", this);
        }
    }

    protected virtual void Update()
    {
        MoveAlongPath();
    }

    protected void MoveAlongPath()
    {
        if (targetWaypoint == null || path == null) return;

        var dir = (targetWaypoint.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;

        float dist = Vector2.Distance(transform.position, targetWaypoint.position);
        if (dist < 0.05f)
        {
            waypointIndex++;
            if (waypointIndex >= path.Count)
            {
                ReachEnd();
            }
            else
            {
                targetWaypoint = path.GetWaypoint(waypointIndex);
            }
        }
    }

    protected virtual void ReachEnd()
    {
        GameManager.Instance?.TakeLifeDamage(damageToBase);
        ownerSpawner?.OnEnemyRemoved(this);
        Destroy(gameObject);
    }

    public virtual void TakeDamage(int dmg)
    {
        currentHealth -= Mathf.Max(0, dmg);
        if (currentHealth <= 0) Die();
    }

    protected virtual void Die()
    {
        GameManager.Instance?.AddGold(goldReward);
        ownerSpawner?.OnEnemyRemoved(this);
        Destroy(gameObject);
    }
}
