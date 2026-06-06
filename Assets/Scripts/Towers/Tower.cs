using UnityEngine;

public enum TowerTargetType { WaterOnly, AirOnly, Any }

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
    public int buildCost = 30;
    [Range(0f, 1f)] public float sellRefundRatio = 0.7f;
    public TowerUpgradeLevel[] upgrades;

    [Header("Targeting")]
    public TowerTargetType targetType = TowerTargetType.WaterOnly;
    public float range = 3.5f;
    public LayerMask enemyLayer;
    public Transform rotatingPart;   // aim this at target
    public Transform firePoint;      // projectile spawn position

    [Header("Firing")]
    public Projectile projectilePrefab;
    public float shotsPerSecond = 1.0f;

    protected float _cooldown;
    protected Enemy _currentTarget;
    private BuildNode buildNode;

    public int UpgradeLevel { get; private set; }
    public BuildNode BuildNode => buildNode;

    protected virtual void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Tower");
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
        Vector2 dir = _currentTarget.transform.position - rotatingPart.position;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rotatingPart.rotation = Quaternion.AngleAxis(ang, Vector3.forward);
    }

    protected void Fire()
    {
        if (!projectilePrefab || !firePoint || !_currentTarget) return;

        var proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        proj.Launch(_currentTarget.transform);

        if (fireSfxKey != SFX.None)
        {
            AudioManager.Instance?.PlaySFX(fireSfxKey);
        }
    }

    protected Enemy AcquireTarget()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);
        Enemy best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            if (!h) continue;
            var e = h.GetComponent<Enemy>();
            if (e && IsValidTarget(e))
            {
                float d = Vector2.Distance(transform.position, e.transform.position);
                if (d < bestDist)
                {
                    best = e; bestDist = d;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
