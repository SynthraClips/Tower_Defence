using System;
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

    [Header("Visual Facing")]
    [Tooltip("Optional child transform to rotate towards the next waypoint. If left empty, a child named 'rotatingPart' is found automatically.")]
    [SerializeField] private Transform rotatingPart;

    [Tooltip("Use this if the boat art points up/down/backwards. For most top-down boat sprites, try 0, 90, -90 or 180.")]
    [SerializeField] private float visualRotationOffsetDegrees = 0f;

    [Tooltip("Keeps older boat prefabs working if they do not have a rotatingPart child. Leave this off when the prefab has a separate rotatingPart.")]
    [SerializeField] private bool rotateWholeBoatWhenNoRotatingPart = false;

    [Tooltip("How fast the boat visual can turn. 0 means snap instantly.")]
    [SerializeField] private float visualTurnSpeedDegrees = 720f;

    [Header("Visual / Collider Safety")]
    [Tooltip("Prevents the boat visual becoming too tiny after definition scale is applied as a multiplier.")]
    [SerializeField] private float minimumVisibleWidth = 0.65f;

    [Tooltip("Keeps the root collider centred over the visible boat so projectiles aim and hit the visible body.")]
    [SerializeField] private bool fitRootColliderToVisual = true;

    [SerializeField] private float colliderBoundsPadding = 0.9f;

    [Tooltip("Recentres the visible boat sprite over the enemy root so rotatingPart pivots around the path position instead of orbiting to one side.")]
    [SerializeField] private bool centreVisualOnPathRoot = true;

    private Vector3 lastUsableFacingDirection = Vector3.right;
    private Vector3 baseLocalScale = Vector3.one;
    private Vector3 baseVisualRootLocalPosition = Vector3.zero;
    private bool hasBaseVisualRootLocalPosition;
    private Quaternion baseRotatingPartLocalRotation = Quaternion.identity;
    private Quaternion baseWholeBoatRotation = Quaternion.identity;

    public override Vector3 AimPosition => spriteRenderer ? spriteRenderer.bounds.center : base.AimPosition;

    protected override void Awake()
    {
        base.Awake();
        type = EnemyType.Water;
        baseLocalScale = transform.localScale;
        baseWholeBoatRotation = transform.rotation;

        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (!spriteRenderer)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (!rotatingPart)
        {
            rotatingPart = FindChildRecursive(transform, "rotatingPart");
        }

        if (rotatingPart)
        {
            ResetVisualPivotToRoot();
            baseVisualRootLocalPosition = rotatingPart.localPosition;
            hasBaseVisualRootLocalPosition = true;
            baseRotatingPartLocalRotation = rotatingPart.localRotation;
        }

        CentreVisualOnPathRoot();
        EnsureMinimumVisibleWidth();
        CentreVisualOnPathRoot();
        FitColliderToVisualBounds();
        ConfigurePathCorridorFromVisual();
    }

    protected override void Start()
    {
        base.Start();
        FaceCurrentPathDirection(true);
    }

    private void LateUpdate()
    {
        FaceCurrentPathDirection(false);
    }

    public override void InitialisePath(Path assignedPath, Spawner assignedSpawner, bool snapToStart = true)
    {
        base.InitialisePath(assignedPath, assignedSpawner, snapToStart);
        FaceCurrentPathDirection(true);
    }

    public void SetTier(BoatTier t)
    {
        tier = t;
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

        transform.localScale = MultiplyScale(baseLocalScale, definition.scale);
        CentreVisualOnPathRoot();
        EnsureMinimumVisibleWidth();
        CentreVisualOnPathRoot();
        FitColliderToVisualBounds();
        ConfigurePathCorridorFromVisual();
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

    private void FaceCurrentPathDirection(bool snap)
    {
        Vector3 direction = DesiredMoveDirection;

        if ((!TargetWaypoint || direction.sqrMagnitude <= 0.000001f) && path != null && path.Count > 1)
        {
            Transform next = path.GetWaypoint(Mathf.Clamp(WaypointIndex, 1, path.Count - 1));
            if (next)
            {
                direction = next.position - transform.position;
            }
        }

        if (direction.sqrMagnitude > 0.000001f)
        {
            lastUsableFacingDirection = direction.normalized;
        }

        RotateVisualTowards(lastUsableFacingDirection, snap);
    }

    private void RotateVisualTowards(Vector3 direction, bool snap)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float targetWorldAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + visualRotationOffsetDegrees;
        Quaternion targetWorldRotation = Quaternion.AngleAxis(targetWorldAngle, Vector3.forward);

        if (rotatingPart)
        {
            Quaternion parentRotation = rotatingPart.parent ? rotatingPart.parent.rotation : Quaternion.identity;
            Quaternion targetLocalRotation = Quaternion.Inverse(parentRotation) * targetWorldRotation * baseRotatingPartLocalRotation;
            rotatingPart.localRotation = ApplyTurnSpeed(rotatingPart.localRotation, targetLocalRotation, snap);
        }
        else if (rotateWholeBoatWhenNoRotatingPart)
        {
            Quaternion targetRotation = targetWorldRotation * baseWholeBoatRotation;
            transform.rotation = ApplyTurnSpeed(transform.rotation, targetRotation, snap);
        }
    }

    private Quaternion ApplyTurnSpeed(Quaternion currentRotation, Quaternion targetRotation, bool snap)
    {
        if (snap || visualTurnSpeedDegrees <= 0f)
        {
            return targetRotation;
        }

        return Quaternion.RotateTowards(
            currentRotation,
            targetRotation,
            visualTurnSpeedDegrees * Time.deltaTime);
    }

    private void ApplyLegacyValues(int health, float speed, int baseDamage, int reward, Color tint, Vector3 scaleMultiplier)
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

        transform.localScale = MultiplyScale(baseLocalScale, scaleMultiplier);
        CentreVisualOnPathRoot();
        EnsureMinimumVisibleWidth();
        CentreVisualOnPathRoot();
        FitColliderToVisualBounds();
        ConfigurePathCorridorFromVisual();
    }

    private void CentreVisualOnPathRoot()
    {
        if (!centreVisualOnPathRoot || !spriteRenderer)
        {
            return;
        }

        Transform visualRoot = rotatingPart ? rotatingPart : spriteRenderer.transform;
        if (!visualRoot || visualRoot == transform)
        {
            return;
        }

        Vector3 visualOffset = spriteRenderer.bounds.center - transform.position;
        visualOffset.z = 0f;
        if (visualOffset.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Vector3 localOffset = transform.InverseTransformVector(visualOffset);
        if (visualRoot == rotatingPart && hasBaseVisualRootLocalPosition)
        {
            rotatingPart.localPosition = baseVisualRootLocalPosition - localOffset;
        }
        else
        {
            visualRoot.localPosition -= localOffset;
        }
    }

    private void EnsureMinimumVisibleWidth()
    {
        if (!spriteRenderer || minimumVisibleWidth <= 0f)
        {
            return;
        }

        float width = spriteRenderer.bounds.size.x;
        if (width <= 0.0001f || width >= minimumVisibleWidth)
        {
            return;
        }

        float multiplier = minimumVisibleWidth / width;
        transform.localScale = new Vector3(
            transform.localScale.x * multiplier,
            transform.localScale.y * multiplier,
            transform.localScale.z);
    }

    private void FitColliderToVisualBounds()
    {
        if (!fitRootColliderToVisual || !spriteRenderer)
        {
            return;
        }

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (!box)
        {
            return;
        }

        Bounds bounds = spriteRenderer.bounds;
        if (bounds.size.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, bounds.center.z),
            new Vector3(min.x, max.y, bounds.center.z),
            new Vector3(max.x, min.y, bounds.center.z),
            new Vector3(max.x, max.y, bounds.center.z),
        };

        Vector2 localMin = transform.InverseTransformPoint(corners[0]);
        Vector2 localMax = localMin;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 local = transform.InverseTransformPoint(corners[i]);
            localMin = Vector2.Min(localMin, local);
            localMax = Vector2.Max(localMax, local);
        }

        Vector2 size = localMax - localMin;
        if (size.x <= 0.0001f || size.y <= 0.0001f)
        {
            return;
        }

        box.offset = (localMin + localMax) * 0.5f;
        box.size = size * Mathf.Clamp(colliderBoundsPadding, 0.5f, 1.5f);
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            Transform nestedMatch = FindChildRecursive(child, childName);
            if (nestedMatch)
            {
                return nestedMatch;
            }
        }

        return null;
    }

    private static Vector3 MultiplyScale(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    private void ConfigurePathCorridorFromVisual()
    {
        float suggestedRadius = 0.65f;
        if (spriteRenderer)
        {
            suggestedRadius = Mathf.Clamp(spriteRenderer.bounds.size.x * 0.32f, 0.6f, 1.1f);
        }

        ConfigurePathCorridor(suggestedRadius, 0f, true);
    }

    private void ResetVisualPivotToRoot()
    {
        if (!rotatingPart)
        {
            return;
        }

        rotatingPart.localPosition = Vector3.zero;
        if (spriteRenderer && spriteRenderer.transform.parent == rotatingPart)
        {
            spriteRenderer.transform.localPosition = Vector3.zero;
        }
    }

}
