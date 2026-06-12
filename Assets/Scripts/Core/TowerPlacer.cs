using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TowerPlacer : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public BuildManager build;
    public LineRenderer rangePreviewPrefab; // optional

    private Tower selectedTowerPrefab;
    private Tower ghostInstance;
    private SpriteRenderer[] ghostSprites;
    private LineRenderer rangePreview;
    private SpriteRenderer rangeFillPreview;
    private float selectedTowerRange;
    private bool    isPlacing;
    private int     cachedCost;
    private PlacementValidationResult lastValidationResult = PlacementValidationResult.OutOfBounds;

    private void Awake()
    {
        RefreshDependencies();
    }

    private void Update()
    {
        if (!isPlacing) return;

        RefreshDependencies();
        if (!build || !GameManager.Instance)
        {
            return;
        }

        // Cancel
        if (WasRightMousePressedThisFrame() || WasEscapePressedThisFrame())
        {
            CancelPlacement();
            return;
        }

        // Mouse world pos
        if (!TryGetMouseWorldOnGround(out Vector3 posWorld)) return;

        if (build.snapToGrid) posWorld = BuildManager.Snap(posWorld, build.gridSize);

        // Validate
        lastValidationResult = build != null
            ? build.ValidatePlacement(posWorld)
            : PlacementValidationResult.OutOfBounds;
        bool ok = lastValidationResult == PlacementValidationResult.Valid;

        // Move ghost + color
        if (ghostInstance)
        {
            ghostInstance.transform.position = posWorld;
            SetGhostColor(ok ? build.okColor : build.badColor);
        }

        UpdateRangePreview(selectedTowerRange);

        // Place
        if (ok && WasLeftMousePressedThisFrame() && GameManager.Instance.gold >= cachedCost)
        {
            if (build.TrySpend(cachedCost))
            {
                PlaceTower(posWorld);
                EndPlacement(); // optional: end after one, or comment to keep placing
            }
            else
            {
                Debug.Log("Not enough gold.");
            }
        }
        else if (!ok && WasLeftMousePressedThisFrame())
        {
            Debug.Log($"Cannot place tower here: {lastValidationResult}.");
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
        selectedTowerPrefab = towerPrefab;
        cachedCost = Mathf.Max(0, towerPrefab.buildCost);
        selectedTowerRange = Mathf.Max(0.1f, towerPrefab.range);
        RefreshDependencies();
        if (!build)
        {
            Debug.LogWarning("[TowerPlacer] BuildManager is missing, cannot begin placement.", this);
            isPlacing = false;
            selectedTowerPrefab = null;
            selectedTowerRange = 0f;
            return;
        }

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
            rangePreview.transform.SetParent(null, false);
            rangePreview.transform.localScale = Vector3.one;
            rangePreview.loop = true;
            rangePreview.useWorldSpace = true;
            rangePreview.positionCount = 65;
            if (rangePreview.sharedMaterial == null && Shader.Find("Sprites/Default") != null)
            {
                rangePreview.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            rangePreview.widthMultiplier = Mathf.Clamp(rangePreview.widthMultiplier, 0.03f, 0.15f);
        }
        else
        {
            rangePreview = CreateRuntimeRangePreview();
        }

        rangeFillPreview = CreateRuntimeRangeFillPreview();

        UpdateRangePreview(selectedTowerRange);
    }

    private void EndPlacement()
    {
        CancelPlacement(); // or keep ghost for multi-place
    }

    private void CancelPlacement()
    {
        isPlacing = false;
        if (ghostInstance) Destroy(ghostInstance.gameObject);
        if (rangePreview) Destroy(rangePreview.gameObject);
        if (rangeFillPreview) Destroy(rangeFillPreview.gameObject);
        selectedTowerPrefab = null;
        selectedTowerRange = 0f;
        ghostInstance = null;
        rangePreview = null;
        rangeFillPreview = null;
        ghostSprites = null;
        lastValidationResult = PlacementValidationResult.OutOfBounds;
    }

    private void PlaceTower(Vector3 pos)
    {
        if (!selectedTowerPrefab)
        {
            Debug.LogWarning("No selected tower prefab was available when placing.");
            return;
        }

        // Instantiate from the original prefab, not the ghost.
        // This prevents the real tower inheriting disabled colliders, disabled scripts or ghost tint alpha.
        var real = Instantiate(selectedTowerPrefab, pos, selectedTowerPrefab.transform.rotation);

        var realCol = real.GetComponent<Collider2D>();
        if (realCol) realCol.enabled = true;
        real.enabled = true;

        if (build && build.requireBuildNodes)
        {
            BuildNode node = build.GetBuildNodeAt(pos);
            if (node && !node.TryOccupy(real))
            {
                Debug.LogWarning($"Could not occupy build node at {pos} for {real.name}.");
            }
        }

        // Mark on Tower layer for blockers
        int towerLayer = LayerMask.NameToLayer("Tower");
        if (towerLayer >= 0) real.gameObject.layer = towerLayer;

        Debug.Log($"Placed {selectedTowerPrefab.GetType().Name} at {pos} for {cachedCost} gold.");
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
        int segments = Mathf.Max(4, rangePreview.positionCount - 1);
        for (int i = 0; i <= segments; i++)
        {
            float t = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f) * radius;
            rangePreview.SetPosition(i, p);
        }

        if (rangeFillPreview)
        {
            rangeFillPreview.transform.position = center;
            float diameter = radius * 2f;
            rangeFillPreview.transform.localScale = new Vector3(diameter, diameter, 1f);
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

    private void RefreshDependencies()
    {
        if (!cam)
        {
            cam = Camera.main;
        }

        if (!build)
        {
            build = BuildManager.Instance;
            if (!build)
            {
                build = FindAnyObjectByType<BuildManager>();
            }
        }
    }

    private static LineRenderer CreateRuntimeRangePreview()
    {
        var previewObject = new GameObject("RuntimeRangePreview");
        var renderer = previewObject.AddComponent<LineRenderer>();
        renderer.loop = true;
        renderer.useWorldSpace = true;
        renderer.positionCount = 65;
        renderer.startWidth = 0.06f;
        renderer.endWidth = 0.06f;
        renderer.widthMultiplier = 1f;
        renderer.numCornerVertices = 4;
        renderer.numCapVertices = 4;
        renderer.startColor = new Color(0.56f, 0.96f, 1f, 0.95f);
        renderer.endColor = renderer.startColor;
        renderer.sortingOrder = 50;
        if (Shader.Find("Sprites/Default") != null)
        {
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        return renderer;
    }

    private static SpriteRenderer CreateRuntimeRangeFillPreview()
    {
        var fillObject = new GameObject("RuntimeRangePreviewFill");
        var renderer = fillObject.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateCircleSprite();
        renderer.color = new Color(0.3f, 0.8f, 1f, 0.16f);
        renderer.sortingOrder = 49;
        return renderer;
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeRangePreviewCircle";
        Vector2 centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), centre);
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
