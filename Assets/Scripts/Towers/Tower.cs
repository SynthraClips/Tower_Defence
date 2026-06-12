using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public enum TowerTargetType { WaterOnly, AirOnly, Any }
public enum TowerArchetype { Light, Heavy, Magic, Air }
public enum AttackPreferenceMode { Close, Weak, Far }

public abstract class Tower : MonoBehaviour
{
    [System.Serializable]
    public struct TowerUpgradeLevel
    {
        [Min(0)] public int upgradeCost;
        [Min(0)] public int sellValue;
        [Min(0f)] public float range;
        [Min(0.01f)] public float shotsPerSecond;
        public Projectile projectilePrefab;
    }

    [Header("Audio")]
    [SerializeField] private SFX fireSfxKey = SFX.None;

    [Header("Build")]
    public string towerDisplayName = "Tower";
    public TowerArchetype towerArchetype = TowerArchetype.Light;
    public DamageType damageType = DamageType.Physical;
    public AttackPreferenceMode attackPreferenceMode = AttackPreferenceMode.Close;
    public int buildCost = 30;
    [Range(0f, 1f)] public float sellRefundRatio = 0.7f;
    public TowerUpgradeLevel[] upgrades;
    [FormerlySerializedAs("shopSprite")]
    public Sprite shopIcon;

    [Header("Targeting")]
    public TowerTargetType targetType = TowerTargetType.WaterOnly;
    public float range = 3.5f;
    public LayerMask enemyLayer;
    public Transform rotatingPart;   // aim this at target
    public Transform firePoint;      // projectile spawn position
    [SerializeField] private bool autoCentreRotatingPartOnVisual = true;
    [SerializeField] private float rotationSmoothingDegreesPerSecond = 540f;

    [Header("Firing")]
    public Projectile projectilePrefab;
    public float shotsPerSecond = 1.0f;

    [Header("Fallback Visual")]
    [SerializeField] private bool createFallbackSpriteWhenMissing = false;
    [SerializeField] private Color fallbackSpriteColour = new Color(0.35f, 0.55f, 0.85f, 1f);
    [SerializeField] private Vector2 fallbackSpriteSize = new Vector2(0.75f, 0.75f);

    protected float _cooldown;
    protected Enemy _currentTarget;
    private BuildNode buildNode;
    private Quaternion baseRotatingPartLocalRotation = Quaternion.identity;
    private bool hasBaseRotatingPartRotation;
    private static Sprite fallbackSprite;

    public int UpgradeLevel { get; private set; }
    public BuildNode BuildNode => buildNode;

    protected virtual void Awake()
    {
        if (!rotatingPart)
        {
            rotatingPart = FindChildRecursive(transform, "rotatingPart");
        }

        if (!firePoint)
        {
            firePoint = FindChildRecursive(transform, "FirePoint");
        }

        if (rotatingPart)
        {
            NormaliseRotatingPartPivot();
            CentreRotatingPartOnVisual();
            baseRotatingPartLocalRotation = rotatingPart.localRotation;
            hasBaseRotatingPartRotation = true;
        }

        EnsureFallbackVisual();
    }

    protected virtual void Start()
    {
        int towerLayer = LayerMask.NameToLayer("Tower");
        if (towerLayer >= 0)
        {
            gameObject.layer = towerLayer;
        }
    }

    protected virtual void Update()
    {
        _cooldown -= Time.deltaTime;

        if (_currentTarget == null || !IsValidTarget(_currentTarget))
            _currentTarget = AcquireTarget();

        AimAtTarget();

        if (_currentTarget && _cooldown <= 0f)
        {
            Fire();
            _cooldown = 1f / Mathf.Max(0.01f, shotsPerSecond);
        }
    }

    protected virtual void OnEnable()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnGameOver += FreezeOnRunEnded;
            GameManager.Instance.OnVictory += FreezeOnRunEnded;
        }
    }

    protected virtual void OnDisable()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnGameOver -= FreezeOnRunEnded;
            GameManager.Instance.OnVictory -= FreezeOnRunEnded;
        }
    }

    public void AssignBuildNode(BuildNode node)
    {
        buildNode = node;
    }

    public bool CanUpgrade()
    {
        return upgrades != null && UpgradeLevel < upgrades.Length;
    }

    public int GetUpgradeCost()
    {
        return CanUpgrade() ? Mathf.Max(0, upgrades[UpgradeLevel].upgradeCost) : 0;
    }

    public int GetSellValue()
    {
        if (upgrades == null || upgrades.Length == 0 || UpgradeLevel == 0)
        {
            return Mathf.RoundToInt(buildCost * sellRefundRatio);
        }

        int configuredSellValue = upgrades[Mathf.Clamp(UpgradeLevel - 1, 0, upgrades.Length - 1)].sellValue;
        if (configuredSellValue > 0)
        {
            return configuredSellValue;
        }

        int invested = buildCost;
        for (int i = 0; i < UpgradeLevel; i++)
        {
            invested += Mathf.Max(0, upgrades[i].upgradeCost);
        }

        return Mathf.RoundToInt(invested * sellRefundRatio);
    }

    public bool TryUpgrade()
    {
        if (!CanUpgrade())
        {
            return false;
        }

        int cost = GetUpgradeCost();
        if (cost > 0 && !(GameManager.Instance && GameManager.Instance.SpendGold(cost)))
        {
            return false;
        }

        var next = upgrades[UpgradeLevel];
        range = Mathf.Max(range, next.range);
        shotsPerSecond = Mathf.Max(0.01f, next.shotsPerSecond);
        if (next.projectilePrefab)
        {
            projectilePrefab = next.projectilePrefab;
        }

        UpgradeLevel++;
        return true;
    }

    public bool TrySell()
    {
        int sellValue = GetSellValue();
        if (sellValue > 0)
        {
            GameManager.Instance?.AddGold(sellValue);
        }

        buildNode?.ClearOccupancy(this);
        Destroy(gameObject);
        return true;
    }

    private void FreezeOnRunEnded()
    {
        enabled = false;
    }

    protected virtual void AimAtTarget()
    {
        if (!_currentTarget || !rotatingPart) return;
        Vector2 dir = _currentTarget.AimPosition - rotatingPart.position;
        if (dir.sqrMagnitude <= 0.000001f) return;

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion targetWorldRotation = Quaternion.AngleAxis(ang, Vector3.forward);
        Quaternion parentRotation = rotatingPart.parent ? rotatingPart.parent.rotation : Quaternion.identity;
        Quaternion baseRotation = hasBaseRotatingPartRotation ? baseRotatingPartLocalRotation : Quaternion.identity;
        Quaternion targetLocalRotation = Quaternion.Inverse(parentRotation) * targetWorldRotation * baseRotation;
        rotatingPart.localRotation = rotationSmoothingDegreesPerSecond <= 0f
            ? targetLocalRotation
            : Quaternion.RotateTowards(rotatingPart.localRotation, targetLocalRotation, rotationSmoothingDegreesPerSecond * Time.deltaTime);
    }

    protected void Fire()
    {
        if (!projectilePrefab || !_currentTarget) return;

        Transform spawnTransform = firePoint ? firePoint : rotatingPart ? rotatingPart : transform;
        var proj = Instantiate(projectilePrefab, spawnTransform.position, spawnTransform.rotation);
        proj.damageType = damageType;
        proj.Launch(_currentTarget);

        AudioManager.Instance?.PlaySFX(ResolveFireSfx());
    }

    protected Enemy AcquireTarget()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);
        Enemy best = null;
        float bestScore = float.MaxValue;
        var seenEnemies = new HashSet<Enemy>();

        foreach (var h in hits)
        {
            if (!h) continue;
            var e = h.GetComponentInParent<Enemy>();
            if (e && seenEnemies.Add(e) && IsValidTarget(e))
            {
                float score = GetTargetScore(e);
                if (score < bestScore)
                {
                    best = e;
                    bestScore = score;
                }
            }
        }
        return best;
    }

    protected bool IsValidTarget(Enemy e)
    {
        if (!e) return false;
        return targetType switch
        {
            TowerTargetType.WaterOnly => e.type == EnemyType.Water,
            TowerTargetType.AirOnly   => e.type == EnemyType.Air,
            _                         => true,
        };
    }

    private float GetTargetScore(Enemy enemy)
    {
        float distance = Vector2.Distance(transform.position, enemy.AimPosition);
        return attackPreferenceMode switch
        {
            AttackPreferenceMode.Weak => enemy.CurrentHealth * 1000f + distance,
            AttackPreferenceMode.Far => -enemy.WaypointIndex * 1000f + enemy.DistanceToCurrentWaypoint,
            _ => distance,
        };
    }

    private SFX ResolveFireSfx()
    {
        if (fireSfxKey != SFX.None)
        {
            return fireSfxKey;
        }

        return towerArchetype switch
        {
            TowerArchetype.Heavy => SFX.Cannon,
            TowerArchetype.Magic => SFX.MagicFire,
            TowerArchetype.Air => SFX.AirFire,
            _ => SFX.Ballista,
        };
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
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

    private void CentreRotatingPartOnVisual()
    {
        if (!autoCentreRotatingPartOnVisual || !rotatingPart)
        {
            return;
        }

        SpriteRenderer visualRenderer = rotatingPart.GetComponentInChildren<SpriteRenderer>();
        if (!visualRenderer)
        {
            return;
        }

        Vector3 offset = visualRenderer.bounds.center - rotatingPart.position;
        offset.z = 0f;
        if (offset.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        rotatingPart.position += offset;
        if (firePoint)
        {
            firePoint.position += offset;
        }
    }

    private void NormaliseRotatingPartPivot()
    {
        if (!rotatingPart || rotatingPart.parent != transform)
        {
            return;
        }

        Vector3 pivotOffset = rotatingPart.localPosition;
        if (pivotOffset.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        var children = new List<Transform>();
        foreach (Transform child in rotatingPart)
        {
            children.Add(child);
        }

        rotatingPart.localPosition = Vector3.zero;
        foreach (Transform child in children)
        {
            child.localPosition += pivotOffset;
        }
    }

    private void EnsureFallbackVisual()
    {
        if (!createFallbackSpriteWhenMissing)
        {
            return;
        }

        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
        if (renderer && renderer.sprite)
        {
            return;
        }

        Transform parent = rotatingPart ? rotatingPart : transform;
        var fallbackObject = new GameObject("FallbackTowerVisual");
        fallbackObject.transform.SetParent(parent, false);
        fallbackObject.transform.localPosition = Vector3.zero;
        fallbackObject.transform.localRotation = Quaternion.identity;
        fallbackObject.transform.localScale = new Vector3(fallbackSpriteSize.x, fallbackSpriteSize.y, 1f);

        var fallbackRenderer = fallbackObject.AddComponent<SpriteRenderer>();
        fallbackRenderer.sprite = GetFallbackSprite();
        fallbackRenderer.color = fallbackSpriteColour;
        fallbackRenderer.sortingOrder = 1;
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite)
        {
            return fallbackSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "RuntimeTowerFallbackSprite";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        fallbackSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        fallbackSprite.name = "RuntimeTowerFallbackSprite";
        return fallbackSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
