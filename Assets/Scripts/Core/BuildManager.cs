using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    [Header("Placement")]
    public bool requireBuildNodes = false;
    public LayerMask buildNodeLayer;
    public LayerMask placeableGround;   // e.g. "Ground"
    public LayerMask waterLayers;       // e.g. "Water"
    public LayerMask blockedLayers;     // e.g. "NoBuild" | "Enemy" | "Tower"
    public float   minClearRadius = 0.35f; // no overlap radius check
    public bool    snapToGrid = true;
    public float   gridSize = 0.5f;

    [Header("Ghost")]
    public Color okColor   = new Color(0.6f, 1f, 0.6f, 0.6f);
    public Color badColor  = new Color(1f, 0.5f, 0.5f, 0.6f);

    [HideInInspector] public Tower towerToBuild; // selected prefab
    private SpriteRenderer[] groundRenderers = System.Array.Empty<SpriteRenderer>();
    private SpriteRenderer[] waterRenderers = System.Array.Empty<SpriteRenderer>();

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CachePlacementSurfaces();
    }

    public bool TrySpend(int cost) => GameManager.Instance && GameManager.Instance.SpendGold(cost);

    public BuildNode GetBuildNodeAt(Vector3 position)
    {
        var hit = Physics2D.OverlapPoint(position, buildNodeLayer);
        return hit ? hit.GetComponent<BuildNode>() : null;
    }

    public bool IsPlacementValid(Vector3 position)
    {
        if (requireBuildNodes)
        {
            var node = GetBuildNodeAt(position);
            return node && node.CanBuild;
        }

        if (!IsOnBuildableGround(position) || IsOnWater(position))
        {
            return false;
        }

        var hit = Physics2D.OverlapCircle(position, minClearRadius, blockedLayers);
        return hit == null;
    }

    public void CachePlacementSurfaces()
    {
        var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var groundList = new System.Collections.Generic.List<SpriteRenderer>();
        var waterList = new System.Collections.Generic.List<SpriteRenderer>();

        foreach (var renderer in renderers)
        {
            if (!renderer || !renderer.enabled) continue;

            int layerMask = 1 << renderer.gameObject.layer;
            if ((placeableGround.value & layerMask) != 0)
            {
                groundList.Add(renderer);
            }
            if ((waterLayers.value & layerMask) != 0)
            {
                waterList.Add(renderer);
            }
        }

        groundRenderers = groundList.ToArray();
        waterRenderers = waterList.ToArray();
    }

    private bool IsOnBuildableGround(Vector3 position)
    {
        if (groundRenderers.Length == 0)
        {
            CachePlacementSurfaces();
        }

        foreach (var renderer in groundRenderers)
        {
            if (renderer && renderer.bounds.Contains(position))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOnWater(Vector3 position)
    {
        if (waterRenderers.Length == 0)
        {
            CachePlacementSurfaces();
        }

        foreach (var renderer in waterRenderers)
        {
            if (renderer && renderer.bounds.Contains(position))
            {
                return true;
            }
        }

        return false;
    }

    public static Vector3 Snap(Vector3 pos, float size)
    {
        pos.x = Mathf.Round(pos.x / size) * size;
        pos.y = Mathf.Round(pos.y / size) * size;
        pos.z = 0f;
        return pos;
    }
}
