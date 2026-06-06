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
        var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude);
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
            if (RendererContainsOpaquePixel(renderer, position))
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
            if (RendererContainsOpaquePixel(renderer, position))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RendererContainsOpaquePixel(SpriteRenderer renderer, Vector3 worldPosition)
    {
        if (!renderer || !renderer.enabled || renderer.sprite == null)
        {
            return false;
        }

        if (!renderer.bounds.Contains(worldPosition))
        {
            return false;
        }

        var sprite = renderer.sprite;
        var texture = sprite.texture;
        if (texture == null)
        {
            return false;
        }

        if (!texture.isReadable)
        {
            // Fallback if a texture import changes unexpectedly.
            return true;
        }

        Vector3 local = renderer.transform.InverseTransformPoint(worldPosition);
        float pixelsPerUnit = sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot;

        float pixelX = local.x * pixelsPerUnit + pivot.x;
        float pixelY = local.y * pixelsPerUnit + pivot.y;

        if (renderer.flipX)
        {
            pixelX = sprite.rect.width - pixelX;
        }

        if (renderer.flipY)
        {
            pixelY = sprite.rect.height - pixelY;
        }

        if (pixelX < 0 || pixelY < 0 || pixelX >= sprite.rect.width || pixelY >= sprite.rect.height)
        {
            return false;
        }

        int textureX = Mathf.FloorToInt(sprite.rect.x + pixelX);
        int textureY = Mathf.FloorToInt(sprite.rect.y + pixelY);
        Color pixel = texture.GetPixel(textureX, textureY);
        return pixel.a > 0.1f;
    }

    public static Vector3 Snap(Vector3 pos, float size)
    {
        pos.x = Mathf.Round(pos.x / size) * size;
        pos.y = Mathf.Round(pos.y / size) * size;
        pos.z = 0f;
        return pos;
    }
}
