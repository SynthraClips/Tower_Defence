using UnityEngine;

public enum EnemyType { Water, Air }
public enum DamageType { Physical, Siege, Arcane, Airburst }

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
    private SpriteRenderer cachedSpriteRenderer;
    private Coroutine damageFlashRoutine;

    public int CurrentHealth => currentHealth;
    public int WaypointIndex => waypointIndex;
    public float DistanceToCurrentWaypoint => targetWaypoint ? Vector2.Distance(transform.position, targetWaypoint.position) : 0f;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        cachedSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
        FloatingPopupSystem.Instance.ShowWorldPopup(
            transform.position,
            $"-{damageToBase} life",
            new Color(1f, 0.42f, 0.42f, 1f),
            Camera.main);
        AudioManager.Instance?.PlaySFX(SFX.BaseHit);
        ownerSpawner?.OnEnemyRemoved(this);
        Destroy(gameObject);
    }

    public virtual void TakeDamage(int dmg, DamageType damageType = DamageType.Physical)
    {
        int finalDamage = ResolveIncomingDamage(dmg, damageType);
        currentHealth -= finalDamage;
        if (finalDamage > 0)
        {
            FloatingPopupSystem.Instance.ShowWorldPopup(
                transform.position,
                $"-{finalDamage}",
                new Color(1f, 0.92f, 0.76f, 1f),
                Camera.main,
                0.45f);
            AudioManager.Instance?.PlaySFX(SFX.BoatHit);
            StartDamageFlash();
        }

        if (currentHealth <= 0) Die();
    }

    protected virtual int ResolveIncomingDamage(int incomingDamage, DamageType damageType)
    {
        return Mathf.Max(0, incomingDamage);
    }

    protected virtual void Die()
    {
        GameManager.Instance?.AddGold(goldReward);
        FloatingPopupSystem.Instance.ShowWorldPopup(
            transform.position,
            $"+{goldReward} gold",
            new Color(1f, 0.86f, 0.35f, 1f),
            Camera.main,
            0.7f,
            new Vector3(0f, 0.8f, 0f));
        AudioManager.Instance?.PlaySFX(SFX.BoatDeath);
        ownerSpawner?.OnEnemyRemoved(this);
        Destroy(gameObject);
    }

    private void StartDamageFlash()
    {
        if (!cachedSpriteRenderer)
        {
            return;
        }

        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
        }

        damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private System.Collections.IEnumerator DamageFlashRoutine()
    {
        if (!cachedSpriteRenderer)
        {
            yield break;
        }

        Color originalColor = cachedSpriteRenderer.color;
        cachedSpriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (cachedSpriteRenderer)
        {
            cachedSpriteRenderer.color = originalColor;
        }

        damageFlashRoutine = null;
    }
}
