using UnityEngine;
using System;

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

    [Header("Pathing")]
    [SerializeField] private float waypointArrivalDistance = 0.05f;
    [SerializeField] private bool snapToPathStartOnInitialise = true;

    [Header("Path Boundary")]
    [Tooltip("Keeps the enemy root inside a safe corridor around the active waypoint segment if physics or prefab offsets ever push it away from the path.")]
    [SerializeField] private bool keepInsidePathCorridor = false;

    [Tooltip("World-space distance from the centre line of the current waypoint segment before the enemy is clamped back in. This should be smaller than half the visible water route width.")]
    [SerializeField] private float pathCorridorRadius = 0.65f;

    [Tooltip("How fast to correct an enemy that has moved outside the path corridor. 0 snaps instantly to the corridor edge. Disabled by default because the waypoint path itself is the reliable source of movement.")]
    [SerializeField] private float pathCorridorCorrectionSpeed = 0f;

    [Header("Death Feedback")]
    [SerializeField] private GameObject deathExplosionPrefab;
    [SerializeField] private Sprite deathExplosionSprite;
    [SerializeField] private float deathExplosionLifetime = 0.55f;

    [Header("Health Bar")]
    [SerializeField] private bool alwaysShowHealthBar = true;
    [SerializeField] private EnemyHealthBarView healthBarPrefab;
    [SerializeField] private Vector3 healthBarLocalOffset = new Vector3(0f, 1.1f, 0f);

    protected int currentHealth;
    protected int waypointIndex;
    protected Transform targetWaypoint;
    private SpriteRenderer cachedSpriteRenderer;
    private Collider2D cachedCollider;
    private Coroutine damageFlashRoutine;
    private bool pathInitialised;
    private bool isDead;
    private EnemyHealthBarView spawnedHealthBar;

    public event Action<float> OnHealthChanged;
    public event Action OnDied;

    public int CurrentHealth => currentHealth;
    public int WaypointIndex => waypointIndex;
    public Transform TargetWaypoint => targetWaypoint;
    public Vector3 DesiredMoveDirection { get; private set; } = Vector3.right;
    public float DistanceToCurrentWaypoint => targetWaypoint ? Vector2.Distance(transform.position, targetWaypoint.position) : 0f;
    public virtual Vector3 AimPosition => cachedCollider ? cachedCollider.bounds.center : transform.position;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        cachedSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        cachedCollider = GetComponentInChildren<Collider2D>();
        deathExplosionSprite = deathExplosionSprite ? deathExplosionSprite : Resources.Load<Sprite>("VFX/boat_explosion_placeholder");
        EnsureHealthBar();
    }

    protected virtual void Start()
    {
        if (!pathInitialised)
        {
            InitialisePath(path, ownerSpawner, snapToPathStartOnInitialise);
        }
    }

    protected virtual void Update()
    {
        MoveAlongPath();
    }

    public virtual void InitialisePath(Path assignedPath, Spawner assignedSpawner, bool snapToStart = true)
    {
        path = assignedPath;
        if (assignedSpawner != null)
        {
            ownerSpawner = assignedSpawner;
        }

        pathInitialised = true;
        waypointIndex = 0;
        targetWaypoint = null;

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[Enemy] {name} spawned without a valid water path.", this);
            DesiredMoveDirection = Vector3.right;
            return;
        }

        Transform firstWaypoint = path.GetWaypoint(0);
        if (firstWaypoint && snapToStart)
        {
            transform.position = firstWaypoint.position;
        }

        if (path.Count == 1)
        {
            targetWaypoint = firstWaypoint;
            UpdateDesiredMoveDirection();
            return;
        }

        // Spawn at waypoint 0, then immediately target waypoint 1.
        waypointIndex = 1;
        targetWaypoint = path.GetWaypoint(waypointIndex);
        UpdateDesiredMoveDirection();
    }

    protected void MoveAlongPath()
    {
        if (targetWaypoint == null || path == null) return;

        UpdateDesiredMoveDirection();
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWaypoint.position,
            moveSpeed * Time.deltaTime);

        KeepInsidePathCorridor();

        if (Vector2.Distance(transform.position, targetWaypoint.position) <= waypointArrivalDistance)
        {
            AdvanceToNextWaypoint();
        }
    }

    private void AdvanceToNextWaypoint()
    {
        waypointIndex++;
        if (waypointIndex >= path.Count)
        {
            ReachEnd();
            return;
        }

        targetWaypoint = path.GetWaypoint(waypointIndex);
        UpdateDesiredMoveDirection();
    }

    private void UpdateDesiredMoveDirection()
    {
        if (!targetWaypoint)
        {
            return;
        }

        Vector3 direction = targetWaypoint.position - transform.position;
        if (direction.sqrMagnitude > 0.000001f)
        {
            DesiredMoveDirection = direction.normalized;
        }
    }

    private void KeepInsidePathCorridor()
    {
        if (!keepInsidePathCorridor || path == null || path.Count < 2 || !targetWaypoint || pathCorridorRadius <= 0f)
        {
            return;
        }

        int previousWaypointIndex = Mathf.Clamp(waypointIndex - 1, 0, path.Count - 1);
        Transform previousWaypoint = path.GetWaypoint(previousWaypointIndex);
        if (!previousWaypoint || previousWaypoint == targetWaypoint)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 closestPoint = ClosestPointOnSegment(currentPosition, previousWaypoint.position, targetWaypoint.position);
        Vector3 offsetFromPath = currentPosition - closestPoint;
        offsetFromPath.z = 0f;

        float radiusSquared = pathCorridorRadius * pathCorridorRadius;
        if (offsetFromPath.sqrMagnitude <= radiusSquared)
        {
            return;
        }

        Vector3 clampedPosition = closestPoint;
        if (offsetFromPath.sqrMagnitude > 0.000001f)
        {
            clampedPosition += offsetFromPath.normalized * pathCorridorRadius;
        }

        clampedPosition.z = currentPosition.z;
        transform.position = pathCorridorCorrectionSpeed <= 0f
            ? clampedPosition
            : Vector3.MoveTowards(currentPosition, clampedPosition, pathCorridorCorrectionSpeed * Time.deltaTime);
    }

    private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
    {
        Vector3 segment = segmentEnd - segmentStart;
        segment.z = 0f;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= Mathf.Epsilon)
        {
            return segmentStart;
        }

        Vector3 pointOffset = point - segmentStart;
        pointOffset.z = 0f;
        float t = Vector3.Dot(pointOffset, segment) / lengthSquared;
        t = Mathf.Clamp01(t);
        return segmentStart + segment * t;
    }

    protected void ConfigurePathCorridor(float radius, float correctionSpeed = 0f, bool enable = true)
    {
        if (enable)
        {
            keepInsidePathCorridor = true;
        }

        pathCorridorRadius = Mathf.Max(pathCorridorRadius, radius);
        pathCorridorCorrectionSpeed = Mathf.Max(pathCorridorCorrectionSpeed, correctionSpeed);
    }

    protected virtual void ReachEnd()
    {
        GameManager.Instance?.TakeLifeDamage(damageToBase);
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
                AimPosition,
                $"-{finalDamage}",
                new Color(1f, 0.92f, 0.76f, 1f),
                Camera.main,
                0.45f);
            AudioManager.Instance?.PlaySFX(SFX.BoatHit);
            StartDamageFlash();
        }

        NotifyHealthChanged();

        if (currentHealth <= 0) Die();
    }

    protected virtual int ResolveIncomingDamage(int incomingDamage, DamageType damageType)
    {
        return Mathf.Max(0, incomingDamage);
    }

    protected virtual void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        SpawnDeathExplosion();
        GameManager.Instance?.AddGold(goldReward);
        AudioManager.Instance?.PlaySFX(SFX.Explosion);
        AudioManager.Instance?.PlaySFX(SFX.BoatDeath);
        ownerSpawner?.OnEnemyRemoved(this);
        OnDied?.Invoke();
        if (spawnedHealthBar)
        {
            spawnedHealthBar.HandleOwnerDeath();
        }
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

    private void EnsureHealthBar()
    {
        if (!alwaysShowHealthBar || spawnedHealthBar)
        {
            return;
        }

        if (!healthBarPrefab)
        {
            Debug.LogWarning($"[Enemy] Missing healthBarPrefab on '{name}'. Health bar will not be shown.", this);
            return;
        }

        spawnedHealthBar = Instantiate(healthBarPrefab, transform);
        spawnedHealthBar.name = healthBarPrefab.name;

        if (!spawnedHealthBar)
        {
            Debug.LogWarning($"[Enemy] Failed to create health bar for '{name}'.", this);
            return;
        }

        Transform barTransform = spawnedHealthBar.transform;
        barTransform.SetParent(transform, false);
        barTransform.localPosition = ResolveHealthBarLocalOffset();
        barTransform.localRotation = Quaternion.identity;
        barTransform.localScale = Vector3.one;

        spawnedHealthBar.Initialise(this);
        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        float normalised = maxHealth > 0 ? Mathf.Clamp01(currentHealth / (float)maxHealth) : 0f;
        OnHealthChanged?.Invoke(normalised);
    }

    private Vector3 ResolveHealthBarLocalOffset()
    {
        if (healthBarLocalOffset != Vector3.zero)
        {
            return healthBarLocalOffset;
        }

        return new Vector3(0f, 0.42f, 0f);
    }

    private void SpawnDeathExplosion()
    {
        if (deathExplosionPrefab)
        {
            GameObject explosionInstance = Instantiate(deathExplosionPrefab, AimPosition, Quaternion.identity);
            if (explosionInstance.TryGetComponent(out SimpleSpriteEffect configuredEffect))
            {
                configuredEffect.Configure(deathExplosionLifetime);
            }
            else
            {
                SimpleSpriteEffect configuredFallbackEffect = explosionInstance.AddComponent<SimpleSpriteEffect>();
                configuredFallbackEffect.Configure(deathExplosionLifetime);
            }
            return;
        }

        if (!deathExplosionSprite)
        {
            return;
        }

        GameObject effectObject = new GameObject("BoatDeathExplosion");
        effectObject.transform.position = AimPosition;
        effectObject.transform.rotation = Quaternion.identity;
        effectObject.transform.localScale = Vector3.one;

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = deathExplosionSprite;
        renderer.sortingOrder = 20;

        SimpleSpriteEffect effect = effectObject.AddComponent<SimpleSpriteEffect>();
        effect.Configure(deathExplosionLifetime);
    }
}
