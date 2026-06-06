using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TowerPlacer : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public BuildManager build;
    public LineRenderer rangePreviewPrefab; // optional

    private Tower   ghostInstance;
    private SpriteRenderer[] ghostSprites;
    private LineRenderer rangePreview;
    private bool    isPlacing;
    private int     cachedCost;
    private BuildNode hoveredNode;

    private void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!build) build = BuildManager.Instance;
    }

    private void Update()
    {
        if (!isPlacing) return;

        // Cancel
        if (WasRightMousePressedThisFrame() || WasEscapePressedThisFrame())
        {
            CancelPlacement();
            return;
        }

        // Mouse world pos
        if (!TryGetMouseWorldOnGround(out Vector3 posWorld)) return;

        if (build.snapToGrid) posWorld = BuildManager.Snap(posWorld, build.gridSize);
        var previousNode = hoveredNode;
        hoveredNode = build.requireBuildNodes ? build.GetBuildNodeAt(posWorld) : null;
        if (previousNode && previousNode != hoveredNode)
        {
            previousNode.SetHighlightState(null);
        }

        // Validate
        bool ok = IsPlaceValid(posWorld);
        hoveredNode?.SetHighlightState(ok);

        // Move ghost + color
        if (ghostInstance)
        {
            ghostInstance.transform.position = hoveredNode ? hoveredNode.transform.position : posWorld;
            SetGhostColor(ok ? build.okColor : build.badColor);
            UpdateRangePreview(ghostInstance.range);
        }

        // Place
        if (ok && WasLeftMousePressedThisFrame() && GameManager.Instance.gold >= cachedCost)
        {
            if (build.TrySpend(cachedCost))
            {
                PlaceTower(hoveredNode ? hoveredNode.transform.position : posWorld, hoveredNode);
                EndPlacement(); // optional: end after one, or comment to keep placing
            }
            else
            {
                Debug.Log("Not enough gold.");
            }
        }
    }

    // Called by UI buttons
    public void BeginPlacement(Tower towerPrefab)
    {
        if (isPlacing) CancelPlacement();

        if (!towerPrefab)
        {
            Debug.LogWarning("No tower prefab provided.");
            return;
        }

        isPlacing = true;
        cachedCost = Mathf.Max(0, towerPrefab.buildCost);

        // Create ghost
        ghostInstance = Instantiate(towerPrefab);
        ghostInstance.enabled = false;               // disable logic while ghosting
        var coll = ghostInstance.GetComponent<Collider2D>();
        if (coll) coll.enabled = false;

        // Make semi-transparent
        ghostSprites = ghostInstance.GetComponentsInChildren<SpriteRenderer>(true);
        SetGhostColor(build.badColor);

        // Create range preview
        if (rangePreviewPrefab)
        {
            rangePreview = Instantiate(rangePreviewPrefab);
            rangePreview.loop = true;
            rangePreview.positionCount = 64;
        }
    }

    private void EndPlacement()
    {
        CancelPlacement(); // or keep ghost for multi-place
    }

    private void CancelPlacement()
    {
        isPlacing = false;
        hoveredNode?.SetHighlightState(null);
        if (ghostInstance) Destroy(ghostInstance.gameObject);
        if (rangePreview) Destroy(rangePreview.gameObject);
        ghostInstance = null;
        rangePreview = null;
        ghostSprites = null;
        hoveredNode = null;
    }

    private void PlaceTower(Vector3 pos, BuildNode node)
    {
        // Re-enable components on a fresh instance so we don't carry ghost state
        var prefab = ghostInstance; // store to read which type we were placing
        var collGhost = prefab.GetComponent<Collider2D>();
        var scriptType = prefab.GetType();

        // Create a fresh real tower
        var real = Instantiate(prefab, pos, prefab.transform.rotation);
        if (collGhost)
        {
            var realCol = real.GetComponent<Collider2D>();
            if (realCol) realCol.enabled = true;
        }
        real.enabled = true;

        // Mark on Tower layer for blockers
        int towerLayer = LayerMask.NameToLayer("Tower");
        if (towerLayer >= 0) real.gameObject.layer = towerLayer;
        if (node && !node.TryOccupy(real))
        {
            Destroy(real.gameObject);
            GameManager.Instance?.AddGold(cachedCost);
            Debug.LogWarning("[TowerPlacer] Build node became invalid before placement completed.");
            return;
        }

        Debug.Log($"Placed {scriptType.Name} at {pos} for {cachedCost} gold.");
    }

    private bool TryGetMouseWorldOnGround(out Vector3 world)
    {
        world = Vector3.zero;
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return false;

        if (Mouse.current == null) return false;

        Vector3 m = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        m.z = 0f;

        // Optional: require clicks only where your ground exists using a Physics2D raycast:
        // Here we just return the world point directly; uncomment if you use a Ground collider:
        // var hit = Physics2D.Raycast(m, Vector2.zero, 0.1f, build.placeableGround);
        // if (!hit) return false; else world = hit.point;

        world = m;
        return true;
    }

    private bool IsPlaceValid(Vector3 pos)
    {
        if (build.requireBuildNodes)
        {
            return hoveredNode && hoveredNode.CanBuild;
        }

        // Check overlap with blocked layers
        var hit = Physics2D.OverlapCircle(pos, build.minClearRadius, build.blockedLayers);
        return hit == null;
    }

    private void SetGhostColor(Color c)
    {
        if (ghostSprites == null) return;
        foreach (var sr in ghostSprites)
        {
            if (!sr) continue;
            var col = sr.color; col.r = c.r; col.g = c.g; col.b = c.b; col.a = c.a;
            sr.color = col;
        }
    }

    private void UpdateRangePreview(float radius)
    {
        if (!rangePreview) return;
        Vector3 center = ghostInstance ? ghostInstance.transform.position : Vector3.zero;
        for (int i = 0; i < rangePreview.positionCount; i++)
        {
            float t = (i / (float)rangePreview.positionCount) * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(t), Mathf.Sin(t)) * radius;
            rangePreview.SetPosition(i, p);
        }
    }

    private static bool WasLeftMousePressedThisFrame()
    {
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private static bool WasRightMousePressedThisFrame()
    {
        return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
    }

    private static bool WasEscapePressedThisFrame()
    {
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }
}
