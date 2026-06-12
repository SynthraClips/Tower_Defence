using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 10f;
    public int damage = 10;
    public float lifetime = 4f;
    public DamageType damageType = DamageType.Physical;

    [Header("Splash (optional)")]
    public bool splashDamage = false;
    public float splashRadius = 0.6f;
    public LayerMask enemyLayer;

    [Header("Impact")]
    [Tooltip("Safety hit distance used as a backup when a fast projectile reaches the enemy aim point between physics trigger checks.")]
    [SerializeField] private float impactDistance = 0.25f;

    [Header("Visual Facing")]
    [Tooltip("Optional child transform to rotate towards the current target. If left empty, a child named 'rotatingPart' is found automatically.")]
    [SerializeField] private Transform rotatingPart;

    [Tooltip("Use this if the projectile art points up/down/backwards. For most top-down projectile sprites, try 0, 90, -90 or 180.")]
    [SerializeField] private float visualRotationOffsetDegrees = 0f;

    [Tooltip("Keeps older projectile prefabs working if they do not have a rotatingPart child.")]
    [SerializeField] private bool rotateWholeProjectileWhenNoRotatingPart = true;

    [Tooltip("Repairs projectile prefabs whose rotatingPart/visual child was accidentally saved far away from the root.")]
    [SerializeField] private bool autoFixExtremeChildOffsets = true;

    [SerializeField] private float extremeChildOffsetThreshold = 2f;

    private Transform target;
    private Enemy targetEnemy;
    private Rigidbody2D rb;
    private Collider2D projectileCollider;
    private Quaternion baseRotatingPartLocalRotation = Quaternion.identity;
    private Quaternion baseWholeProjectileRotation = Quaternion.identity;
    private bool hasImpacted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        projectileCollider = GetComponent<Collider2D>();
        projectileCollider.isTrigger = true;

        if (!rotatingPart)
        {
            rotatingPart = FindChildRecursive(transform, "rotatingPart");
        }

        RepairExtremeVisualOffsets();

        if (rotatingPart)
        {
            baseRotatingPartLocalRotation = rotatingPart.localRotation;
        }

        baseWholeProjectileRotation = transform.rotation;
    }

    public void Launch(Enemy newTarget)
    {
        targetEnemy = newTarget;
        target = newTarget ? newTarget.transform : null;
        BeginFlight();
    }

    public void Launch(Transform newTarget)
    {
        target = newTarget;
        targetEnemy = newTarget ? newTarget.GetComponentInParent<Enemy>() : null;
        BeginFlight();
    }

    private void BeginFlight()
    {
        hasImpacted = false;
        FaceTargetNow();
        CancelInvoke(nameof(SelfDestruct));
        Invoke(nameof(SelfDestruct), lifetime);
    }

    private void OnEnable()
    {
        if (rb) rb.simulated = true;

        if (GameManager.Instance)
        {
            GameManager.Instance.OnGameOver += FreezeOnRunEnded;
            GameManager.Instance.OnVictory += FreezeOnRunEnded;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnGameOver -= FreezeOnRunEnded;
            GameManager.Instance.OnVictory -= FreezeOnRunEnded;
        }
    }

    private void FreezeOnRunEnded()
    {
        enabled = false;
        if (rb) rb.simulated = false;
    }

    private void Update()
    {
        if (hasImpacted)
        {
            return;
        }

        if (target == null || targetEnemy == null)
        {
            SelfDestruct();
            return;
        }

        Vector3 targetPosition = targetEnemy.AimPosition;
        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude <= 0.000001f)
        {
            ImpactEnemy(targetEnemy);
            return;
        }

        Vector3 normalisedDirection = direction.normalized;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime);

        RotateVisualTowards(normalisedDirection);

        if (Vector2.Distance(transform.position, targetPosition) <= impactDistance)
        {
            ImpactEnemy(targetEnemy);
        }
    }

    private void FaceTargetNow()
    {
        if (targetEnemy == null && target != null)
        {
            targetEnemy = target.GetComponentInParent<Enemy>();
        }

        Vector3 targetPosition = targetEnemy ? targetEnemy.AimPosition : target ? target.position : transform.position;
        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude > 0.000001f)
        {
            RotateVisualTowards(direction.normalized);
        }
    }

    private void RotateVisualTowards(Vector3 direction)
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
            rotatingPart.localRotation = Quaternion.Inverse(parentRotation) * targetWorldRotation * baseRotatingPartLocalRotation;
        }
        else if (rotateWholeProjectileWhenNoRotatingPart)
        {
            transform.rotation = targetWorldRotation * baseWholeProjectileRotation;
        }
    }

    private void RepairExtremeVisualOffsets()
    {
        if (!autoFixExtremeChildOffsets || extremeChildOffsetThreshold <= 0f)
        {
            return;
        }

        float thresholdSquared = extremeChildOffsetThreshold * extremeChildOffsetThreshold;
        if (rotatingPart && rotatingPart.localPosition.sqrMagnitude > thresholdSquared)
        {
            rotatingPart.localPosition = Vector3.zero;
            rotatingPart.localRotation = Quaternion.identity;
            rotatingPart.localScale = Vector3.one;
        }

        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!renderer || renderer.transform == transform)
            {
                continue;
            }

            if (renderer.transform.localPosition.sqrMagnitude > thresholdSquared)
            {
                renderer.transform.localPosition = Vector3.zero;
                renderer.transform.localRotation = Quaternion.identity;
                renderer.transform.localScale = Vector3.one;
            }
        }
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasImpacted)
        {
            return;
        }

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        ImpactEnemy(enemy);
    }

    private void ImpactEnemy(Enemy enemy)
    {
        if (hasImpacted || enemy == null)
        {
            return;
        }

        hasImpacted = true;

        if (!splashDamage)
        {
            enemy.TakeDamage(damage, damageType);
            PlayImpactSfx(false);
        }
        else
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, splashRadius, enemyLayer);
            bool hitAny = false;
            var damagedEnemies = new HashSet<Enemy>();
            foreach (var h in hits)
            {
                var hitEnemy = h.GetComponentInParent<Enemy>();
                if (hitEnemy && damagedEnemies.Add(hitEnemy))
                {
                    hitAny = true;
                    hitEnemy.TakeDamage(damage, damageType);
                }
            }

            if (!hitAny)
            {
                enemy.TakeDamage(damage, damageType);
            }

            FloatingPopupSystem.Instance.ShowWorldPopup(
                transform.position,
                "Splash",
                new Color(0.6f, 0.92f, 1f, 1f),
                Camera.main,
                0.4f);
            PlayImpactSfx(true);
        }

        SelfDestruct();
    }

    private void PlayImpactSfx(bool usedSplash)
    {
        AudioManager.Instance?.PlaySFX(usedSplash ? SFX.WaterSplash : SFX.Impact);
    }

    private void SelfDestruct()
    {
        CancelInvoke();
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (splashDamage)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, splashRadius);
        }
    }
}
