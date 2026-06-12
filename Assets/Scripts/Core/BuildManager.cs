using UnityEngine;

public enum PlacementValidationResult
{
    Valid,
    MissingBuildNode,
    OccupiedOrBlocked,
    OutOfBounds,
    OnWaterRoute,
}

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    [Header("Placement")]
    public bool requireBuildNodes = false;
    public LayerMask buildNodeLayer;
    public LayerMask blockedLayers;
    public float minClearRadius = 0.35f;
    public bool snapToGrid = true;
    public float gridSize = 0.5f;
    [Min(0.1f)] public float waterRouteWidth = 1.55f;

    [Header("Board Area")]
    public bool useManualPlacementBounds = false;
    public Vector2 placementBoundsCenter = Vector2.zero;
    public Vector2 placementBoundsSize = new Vector2(18f, 12f);

    [Header("Ghost")]
    public Color okColor = new Color(0.6f, 1f, 0.6f, 0.6f);
    public Color badColor = new Color(1f, 0.5f, 0.5f, 0.6f);

    [HideInInspector] public Tower towerToBuild;
    [HideInInspector] public Path activePath;

    private Bounds placementBounds;
    private bool hasPlacementBounds;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CachePlacementBounds();
    }

    public bool TrySpend(int cost) => GameManager.Instance && GameManager.Instance.SpendGold(cost);

    public BuildNode GetBuildNodeAt(Vector3 position)
    {
        var hit = Physics2D.OverlapPoint(position, buildNodeLayer);
        return hit ? hit.GetComponent<BuildNode>() : null;
    }

    public bool IsPlacementValid(Vector3 position)
    {
        return ValidatePlacement(position) == PlacementValidationResult.Valid;
    }

    public PlacementValidationResult ValidatePlacement(Vector3 position)
    {
        if (requireBuildNodes)
        {
            var node = GetBuildNodeAt(position);
            return node && node.CanBuild
                ? PlacementValidationResult.Valid
                : PlacementValidationResult.MissingBuildNode;
        }

        if (!IsInsidePlacementBounds(position))
        {
            return PlacementValidationResult.OutOfBounds;
        }

        if (IsOnWaterRoute(position))
        {
            return PlacementValidationResult.OnWaterRoute;
        }

        var hit = Physics2D.OverlapCircle(position, minClearRadius, blockedLayers);
        return hit == null
            ? PlacementValidationResult.Valid
            : PlacementValidationResult.OccupiedOrBlocked;
    }

    public void CachePlacementBounds()
    {
        hasPlacementBounds = false;

        if (useManualPlacementBounds)
        {
            placementBounds = new Bounds(placementBoundsCenter, placementBoundsSize);
            hasPlacementBounds = true;
            return;
        }

        if (!activePath)
        {
            activePath = FindAnyObjectByType<Path>();
        }

        if (activePath && activePath.Count > 0)
        {
            Vector3 min = activePath.GetWaypoint(0).position;
            Vector3 max = min;

            for (int i = 1; i < activePath.Count; i++)
            {
                var waypoint = activePath.GetWaypoint(i);
                if (!waypoint) continue;
                min = Vector3.Min(min, waypoint.position);
                max = Vector3.Max(max, waypoint.position);
            }

            min += new Vector3(-4.5f, -3.5f, 0f);
            max += new Vector3(4.5f, 3.5f, 0f);
            placementBounds = new Bounds((min + max) * 0.5f, max - min);
            hasPlacementBounds = true;
            return;
        }

        var fallbackCamera = Camera.main;
        if (fallbackCamera && fallbackCamera.orthographic)
        {
            float height = fallbackCamera.orthographicSize * 2f;
            float width = height * fallbackCamera.aspect;
            placementBounds = new Bounds(
                new Vector3(fallbackCamera.transform.position.x, fallbackCamera.transform.position.y, 0f),
                new Vector3(width, height, 0f));
            hasPlacementBounds = true;
        }
    }

    private bool IsInsidePlacementBounds(Vector3 position)
    {
        if (!hasPlacementBounds)
        {
            CachePlacementBounds();
        }

        return hasPlacementBounds && placementBounds.Contains(position);
    }

    private bool IsOnWaterRoute(Vector3 position)
    {
        if (!activePath)
        {
            activePath = FindAnyObjectByType<Path>();
        }

        if (!activePath || activePath.Count < 2)
        {
            return false;
        }

        float halfWidth = waterRouteWidth * 0.5f;
        for (int i = 0; i < activePath.Count - 1; i++)
        {
            var a = activePath.GetWaypoint(i);
            var b = activePath.GetWaypoint(i + 1);
            if (!a || !b) continue;

            if (DistancePointToSegment(position, a.position, b.position) <= halfWidth)
            {
                return true;
            }
        }

        return false;
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 segment = segmentEnd - segmentStart;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= Mathf.Epsilon)
        {
            return Vector2.Distance(point, segmentStart);
        }

        float t = Vector2.Dot(point - segmentStart, segment) / lengthSquared;
        t = Mathf.Clamp01(t);
        Vector2 projection = segmentStart + t * segment;
        return Vector2.Distance(point, projection);
    }

    public static Vector3 Snap(Vector3 pos, float size)
    {
        pos.x = Mathf.Round(pos.x / size) * size;
        pos.y = Mathf.Round(pos.y / size) * size;
        pos.z = 0f;
        return pos;
    }
}
