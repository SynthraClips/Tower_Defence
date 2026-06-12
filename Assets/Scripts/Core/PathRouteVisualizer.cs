using UnityEngine;

#pragma warning disable 0414 // Old serialised compatibility fields are intentionally kept.

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class PathRouteVisualizer : MonoBehaviour
{
    [Header("Environment")]
    [SerializeField] private Sprite waterSprite;
    [SerializeField] private Sprite groundSprite;
    [SerializeField] private bool fitGroundToMainCamera = true;
    [SerializeField] private Vector2 groundPadding = new Vector2(2f, 1.5f);
    [SerializeField] private Color groundColor = Color.white;
    [SerializeField] private int groundSortingOrder = -10;
    [SerializeField] private bool disableLegacyWaterBackground = true;

    [Header("Old Water Background - kept for prefab compatibility")]
    [SerializeField, HideInInspector] private bool fitWaterBackgroundToWaypoints = false;
    [SerializeField, HideInInspector] private bool useExistingWaterSpriteRenderer = false;
    [SerializeField, HideInInspector] private Vector2 waterBackgroundPadding = new Vector2(3.5f, 2.5f);
    [SerializeField, HideInInspector] private Color waterBackgroundColor = new Color(1f, 1f, 1f, 0.38f);
    [SerializeField, HideInInspector] private int waterBackgroundSortingOrder = -8;

    [Header("Route Visuals")]
    [SerializeField] private bool showRouteLines = true;
    [Tooltip("For ship-stability, prefer the LineRenderer route over rebuilding lots of sprite segment children.")]
    [SerializeField] private bool preferStableLineRenderer = true;
    [Tooltip("Preferred route renderer. Uses small SpriteRenderer segments instead of a very wide LineRenderer so the route does not tear or spike at corners.")]
    [SerializeField] private bool useSpriteRouteVisuals = true;
    [Tooltip("Optional dark edging behind the water route. Keep this subtle so the route still reads as water.")]
    [SerializeField] private bool showRouteBank = true;
    [Tooltip("Optional centre foam line. Disabled by default because it made the route look like a white debug path.")]
    [SerializeField] private bool showFoamLine = false;
    [SerializeField] private bool showWaypointMarkers = false;
    [SerializeField] private bool showSpawnAndCoreMarkers = false;

    [Header("Route Colours")]
    [SerializeField] private Color routeColor = Color.white;
    [SerializeField] private Color bankColor = new Color(0.04f, 0.20f, 0.26f, 0.65f);
    [SerializeField] private Color foamColor = new Color(0.8f, 0.95f, 1f, 0.75f);
    [SerializeField] private Color spawnOuterColor = new Color(0.12f, 0.86f, 0.95f, 0.95f);
    [SerializeField] private Color spawnInnerColor = new Color(0.9f, 1f, 1f, 0.95f);
    [SerializeField] private Color waypointColor = new Color(0.72f, 0.9f, 1f, 0.45f);
    [SerializeField] private Color coreOuterColor = new Color(0.1f, 0.45f, 0.75f, 0.95f);
    [SerializeField] private Color coreInnerColor = new Color(0.9f, 0.98f, 1f, 0.95f);

    [Header("Route Widths")]
    [SerializeField] private float routeWidth = 1.55f;
    [SerializeField] private float bankWidth = 1.75f;
    [SerializeField] private float foamWidth = 0.12f;
    [SerializeField] private float spawnRadiusOuter = 0.75f;
    [SerializeField] private float spawnRadiusInner = 0.35f;
    [SerializeField] private float waypointMarkerRadius = 0.14f;
    [SerializeField] private float coreRadiusOuter = 0.9f;
    [SerializeField] private float coreRadiusInner = 0.5f;

    [Header("Sorting")]
    [SerializeField] private int bankSortingOrder = -4;
    [SerializeField] private int routeSortingOrder = -3;
    [SerializeField] private int foamSortingOrder = -2;
    [SerializeField] private int coreSortingOrder = -1;

    private Path cachedPath;
    private LineRenderer routeRenderer;
    private LineRenderer bankRenderer;
    private LineRenderer foamRenderer;
    private LineRenderer waypointRenderer;
    private LineRenderer spawnOuterRenderer;
    private LineRenderer spawnInnerRenderer;
    private LineRenderer coreOuterRenderer;
    private LineRenderer coreInnerRenderer;
    private SpriteRenderer groundRenderer;
    private Transform spriteRouteContainer;
    private const int CircleSegments = 40;
    private const string SpriteRouteContainerName = "WaterRouteVisuals";

    private void Awake()
    {
        if (Application.isPlaying)
        {
            Rebuild();
        }
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            QueueRebuild();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            QueueRebuild();
        }
    }
#endif

    public void SetRouteWidth(float width)
    {
        routeWidth = Mathf.Max(0.8f, width);
        bankWidth = Mathf.Max(routeWidth + 0.18f, bankWidth);
        foamWidth = Mathf.Clamp(routeWidth * 0.06f, 0.08f, 0.18f);
        QueueRebuild();
    }

    public void Rebuild()
    {
        if (!this || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
        {
            return;
        }

        cachedPath = GetComponent<Path>();
        routeRenderer = GetComponent<LineRenderer>();

        if (!cachedPath)
        {
            return;
        }

        cachedPath.RebuildFromChildren();
        DisableLegacyWaterBackgroundRenderer();
        EnsureVisualObjects();
        FitGroundBackground();

        if (showRouteLines)
        {
            bool useStableLineRenderer = preferStableLineRenderer || !useSpriteRouteVisuals || !waterSprite;
            if (!useStableLineRenderer)
            {
                DisableRenderer(routeRenderer);
                DisableRenderer(bankRenderer);
                DisableRenderer(foamRenderer);
                RebuildSpriteRouteVisuals();
            }
            else
            {
                ClearSpriteRouteVisuals();
                RebuildLineRouteVisuals();
            }
        }
        else
        {
            ClearSpriteRouteVisuals();
            DisableRenderer(routeRenderer);
            DisableRenderer(bankRenderer);
            DisableRenderer(foamRenderer);
        }

        if (showWaypointMarkers)
        {
            waypointRenderer = waypointRenderer ? waypointRenderer : GetOrCreateChildRenderer("RouteWaypoints");
            ConfigureCircleRenderer(waypointRenderer, waypointColor, foamSortingOrder + 1, 0.08f);
            RebuildWaypointMarkers(waypointRenderer);
        }
        else
        {
            DisableRenderer(waypointRenderer);
            DisableExistingChildRenderer("RouteWaypoints");
        }

        if (showSpawnAndCoreMarkers && cachedPath.Count > 0)
        {
            spawnOuterRenderer = spawnOuterRenderer ? spawnOuterRenderer : GetOrCreateChildRenderer("SpawnOuter");
            spawnInnerRenderer = spawnInnerRenderer ? spawnInnerRenderer : GetOrCreateChildRenderer("SpawnInner");
            coreOuterRenderer = coreOuterRenderer ? coreOuterRenderer : GetOrCreateChildRenderer("HarborCoreOuter");
            coreInnerRenderer = coreInnerRenderer ? coreInnerRenderer : GetOrCreateChildRenderer("HarborCoreInner");

            ConfigureCircleRenderer(spawnOuterRenderer, spawnOuterColor, foamSortingOrder + 2, 0.14f);
            ConfigureCircleRenderer(spawnInnerRenderer, spawnInnerColor, foamSortingOrder + 3, 0.14f);
            ConfigureCircleRenderer(coreOuterRenderer, coreOuterColor, coreSortingOrder);
            ConfigureCircleRenderer(coreInnerRenderer, coreInnerColor, coreSortingOrder + 1);

            Vector3 startPoint = cachedPath.GetWaypoint(0).position;
            Vector3 endPoint = cachedPath.GetWaypoint(cachedPath.Count - 1).position;
            RebuildCircle(spawnOuterRenderer, startPoint, spawnRadiusOuter);
            RebuildCircle(spawnInnerRenderer, startPoint, spawnRadiusInner);
            RebuildCircle(coreOuterRenderer, endPoint, coreRadiusOuter);
            RebuildCircle(coreInnerRenderer, endPoint, coreRadiusInner);
        }
        else
        {
            DisableRenderer(spawnOuterRenderer);
            DisableRenderer(spawnInnerRenderer);
            DisableRenderer(coreOuterRenderer);
            DisableRenderer(coreInnerRenderer);
            DisableExistingChildRenderer("SpawnOuter");
            DisableExistingChildRenderer("SpawnInner");
            DisableExistingChildRenderer("HarborCoreOuter");
            DisableExistingChildRenderer("HarborCoreInner");
        }
    }

    private void QueueRebuild()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall -= DelayedEditorRebuild;
            EditorApplication.delayCall += DelayedEditorRebuild;
            return;
        }
#endif
        Rebuild();
    }

#if UNITY_EDITOR
    private void DelayedEditorRebuild()
    {
        if (this)
        {
            Rebuild();
        }
    }
#endif

    private void EnsureVisualObjects()
    {
        routeRenderer = routeRenderer ? routeRenderer : GetComponent<LineRenderer>();
        groundRenderer = groundRenderer ? groundRenderer : GetOrCreateSpriteChild("GroundBackground");

        if (showRouteLines && !useSpriteRouteVisuals)
        {
            bankRenderer = bankRenderer ? bankRenderer : GetOrCreateChildRenderer("RouteBank");
            foamRenderer = foamRenderer ? foamRenderer : GetOrCreateChildRenderer("RouteFoam");
        }
        else
        {
            DisableExistingChildRenderer("RouteBank");
            DisableExistingChildRenderer("RouteFoam");
        }
    }

    private void DisableLegacyWaterBackgroundRenderer()
    {
        if (!disableLegacyWaterBackground)
        {
            return;
        }

        SpriteRenderer rootSpriteRenderer = GetComponent<SpriteRenderer>();
        if (rootSpriteRenderer)
        {
            rootSpriteRenderer.enabled = false;
        }
    }

    private SpriteRenderer GetOrCreateSpriteChild(string childName)
    {
        var child = transform.Find(childName);
        if (!child)
        {
            var childObject = new GameObject(childName);
            childObject.transform.SetParent(transform, false);
            childObject.transform.localPosition = Vector3.zero;
            child = childObject.transform;
        }

        var renderer = child.GetComponent<SpriteRenderer>();
        if (!renderer)
        {
            renderer = child.gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.enabled = true;
        return renderer;
    }

    private void FitGroundBackground()
    {
        if (!groundRenderer)
        {
            return;
        }

        if (!groundSprite)
        {
            groundRenderer.enabled = false;
            return;
        }

        Bounds bounds = ResolveEnvironmentBounds();
        Vector2 size = new Vector2(
            Mathf.Max(1f, bounds.size.x + groundPadding.x * 2f),
            Mathf.Max(1f, bounds.size.y + groundPadding.y * 2f));

        groundRenderer.enabled = true;
        groundRenderer.sprite = groundSprite;
        groundRenderer.color = groundColor;
        groundRenderer.sortingOrder = groundSortingOrder;
        groundRenderer.transform.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z + 0.5f);
        groundRenderer.transform.rotation = Quaternion.identity;

        Vector2 spriteSize = groundSprite.bounds.size;
        float scaleX = spriteSize.x > 0.001f ? size.x / spriteSize.x : 1f;
        float scaleY = spriteSize.y > 0.001f ? size.y / spriteSize.y : 1f;
        groundRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private Bounds ResolveEnvironmentBounds()
    {
        Camera mainCamera = Camera.main;
        if (fitGroundToMainCamera && mainCamera && mainCamera.orthographic)
        {
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            return new Bounds(mainCamera.transform.position, new Vector3(width, height, 1f));
        }

        if (cachedPath != null && cachedPath.Count > 0)
        {
            Vector3 min = cachedPath.GetWaypoint(0).position;
            Vector3 max = min;
            for (int i = 1; i < cachedPath.Count; i++)
            {
                Transform waypoint = cachedPath.GetWaypoint(i);
                if (!waypoint) continue;

                min = Vector3.Min(min, waypoint.position);
                max = Vector3.Max(max, waypoint.position);
            }

            Vector3 centre = (min + max) * 0.5f;
            Vector3 size = new Vector3(
                Mathf.Max(1f, max.x - min.x),
                Mathf.Max(1f, max.y - min.y),
                1f);
            return new Bounds(centre, size);
        }

        return new Bounds(transform.position, new Vector3(20f, 12f, 1f));
    }

    private void RebuildSpriteRouteVisuals()
    {
        ClearSpriteRouteVisuals();

        if (!waterSprite || cachedPath == null || cachedPath.Count == 0)
        {
            return;
        }

        spriteRouteContainer = GetOrCreateContainer(SpriteRouteContainerName);

        if (showRouteBank)
        {
            BuildRouteSpriteSet("Bank", null, bankWidth, bankColor, bankSortingOrder);
        }

        BuildRouteSpriteSet("Water", waterSprite, routeWidth, routeColor, routeSortingOrder);

        if (showFoamLine)
        {
            BuildLineFoamFallback();
        }
    }

    private Transform GetOrCreateContainer(string childName)
    {
        Transform child = transform.Find(childName);
        if (!child)
        {
            var childObject = new GameObject(childName);
            childObject.transform.SetParent(transform, false);
            childObject.transform.localPosition = Vector3.zero;
            child = childObject.transform;
        }

        child.gameObject.SetActive(true);
        return child;
    }

    private void BuildRouteSpriteSet(string prefix, Sprite sprite, float width, Color color, int sortingOrder)
    {
        if (!spriteRouteContainer || cachedPath == null || cachedPath.Count == 0)
        {
            return;
        }

        Sprite spriteToUse = sprite ? sprite : waterSprite;
        if (!spriteToUse)
        {
            return;
        }

        for (int i = 0; i < cachedPath.Count - 1; i++)
        {
            Transform a = cachedPath.GetWaypoint(i);
            Transform b = cachedPath.GetWaypoint(i + 1);
            if (!a || !b)
            {
                continue;
            }

            Vector3 start = a.position;
            Vector3 end = b.position;
            Vector3 delta = end - start;
            delta.z = 0f;
            float length = delta.magnitude;
            if (length <= 0.001f)
            {
                continue;
            }

            var segment = new GameObject($"{prefix}_Segment_{i:00}_{i + 1:00}");
            segment.transform.SetParent(spriteRouteContainer, false);
            segment.transform.position = (start + end) * 0.5f;
            segment.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
            renderer.sprite = spriteToUse;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = SpriteDrawMode.Simple;

            Vector2 spriteSize = spriteToUse.bounds.size;
            float scaleX = spriteSize.x > 0.001f ? length / spriteSize.x : length;
            float scaleY = spriteSize.y > 0.001f ? width / spriteSize.y : width;
            segment.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        for (int i = 0; i < cachedPath.Count; i++)
        {
            Transform waypoint = cachedPath.GetWaypoint(i);
            if (!waypoint)
            {
                continue;
            }

            var corner = new GameObject($"{prefix}_Corner_{i:00}");
            corner.transform.SetParent(spriteRouteContainer, false);
            corner.transform.position = waypoint.position;
            corner.transform.rotation = Quaternion.identity;

            SpriteRenderer renderer = corner.AddComponent<SpriteRenderer>();
            renderer.sprite = spriteToUse;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = SpriteDrawMode.Simple;

            Vector2 spriteSize = spriteToUse.bounds.size;
            float scaleX = spriteSize.x > 0.001f ? width / spriteSize.x : width;
            float scaleY = spriteSize.y > 0.001f ? width / spriteSize.y : width;
            corner.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }

    private void BuildLineFoamFallback()
    {
        foamRenderer = foamRenderer ? foamRenderer : GetOrCreateChildRenderer("RouteFoam");
        ConfigurePathRenderer(foamRenderer, foamWidth, foamColor, foamSortingOrder, false);
        ApplyPathPositions(foamRenderer);
    }

    private void ClearSpriteRouteVisuals()
    {
        Transform container = spriteRouteContainer ? spriteRouteContainer : transform.Find(SpriteRouteContainerName);
        if (!container)
        {
            return;
        }

        container.gameObject.SetActive(false);

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (!child)
            {
                continue;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(child.gameObject);
            }
            else
#endif
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void RebuildLineRouteVisuals()
    {
        if (!routeRenderer || cachedPath == null)
        {
            return;
        }

        if (showRouteBank)
        {
            bankRenderer = bankRenderer ? bankRenderer : GetOrCreateChildRenderer("RouteBank");
            ConfigurePathRenderer(bankRenderer, bankWidth, bankColor, bankSortingOrder, false);
            ApplyPathPositions(bankRenderer);
        }
        else
        {
            DisableRenderer(bankRenderer);
            DisableExistingChildRenderer("RouteBank");
        }

        ConfigurePathRenderer(routeRenderer, routeWidth, routeColor, routeSortingOrder, false);
        ApplyPathPositions(routeRenderer);

        if (showFoamLine)
        {
            foamRenderer = foamRenderer ? foamRenderer : GetOrCreateChildRenderer("RouteFoam");
            ConfigurePathRenderer(foamRenderer, foamWidth, foamColor, foamSortingOrder, false);
            ApplyPathPositions(foamRenderer);
        }
        else
        {
            DisableRenderer(foamRenderer);
            DisableExistingChildRenderer("RouteFoam");
        }
    }

    private void ApplyPathPositions(LineRenderer renderer)
    {
        if (!renderer || cachedPath == null)
        {
            return;
        }

        renderer.enabled = true;
        renderer.positionCount = cachedPath.Count;
        for (int i = 0; i < cachedPath.Count; i++)
        {
            var waypoint = cachedPath.GetWaypoint(i);
            Vector3 position = waypoint ? waypoint.position : transform.position;
            renderer.SetPosition(i, position);
        }
    }

    private LineRenderer GetOrCreateChildRenderer(string childName)
    {
        var child = transform.Find(childName);
        if (!child)
        {
            var childObject = new GameObject(childName);
            childObject.transform.SetParent(transform, false);
            childObject.transform.localPosition = Vector3.zero;
            child = childObject.transform;
        }

        var renderer = child.GetComponent<LineRenderer>();
        if (!renderer)
        {
            renderer = child.gameObject.AddComponent<LineRenderer>();
        }

        renderer.enabled = true;
        return renderer;
    }

    private void ConfigurePathRenderer(LineRenderer renderer, float width, Color color, int order, bool textured)
    {
        if (!renderer) return;

        renderer.loop = false;
        renderer.useWorldSpace = true;
        renderer.numCornerVertices = 10;
        renderer.numCapVertices = 10;
        renderer.startWidth = width;
        renderer.endWidth = width;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = order;
        renderer.startColor = color;
        renderer.endColor = color;
        renderer.textureMode = LineTextureMode.Tile;

        if (renderer.sharedMaterial == null && Shader.Find("Sprites/Default") != null)
        {
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        if (renderer.sharedMaterial != null)
        {
            renderer.sharedMaterial.mainTexture = textured && waterSprite ? waterSprite.texture : null;
        }
    }

    private void ConfigureCircleRenderer(LineRenderer renderer, Color color, int order, float width = 0.12f)
    {
        if (!renderer) return;

        renderer.loop = true;
        renderer.useWorldSpace = true;
        renderer.numCornerVertices = 4;
        renderer.numCapVertices = 4;
        renderer.startWidth = width;
        renderer.endWidth = width;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = order;
        renderer.startColor = color;
        renderer.endColor = color;
        renderer.textureMode = LineTextureMode.Stretch;

        if (renderer.sharedMaterial == null && Shader.Find("Sprites/Default") != null)
        {
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }
    }

    private void RebuildWaypointMarkers(LineRenderer renderer)
    {
        if (!renderer || cachedPath == null || cachedPath.Count == 0) return;

        renderer.enabled = true;
        renderer.positionCount = cachedPath.Count * CircleSegments;
        for (int waypointIndex = 0; waypointIndex < cachedPath.Count; waypointIndex++)
        {
            var waypoint = cachedPath.GetWaypoint(waypointIndex);
            Vector3 center = waypoint ? waypoint.position : transform.position;
            for (int i = 0; i < CircleSegments; i++)
            {
                float t = i / (float)CircleSegments * Mathf.PI * 2f;
                int index = waypointIndex * CircleSegments + i;
                renderer.SetPosition(index, center + new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f) * waypointMarkerRadius);
            }
        }
    }

    private void RebuildCircle(LineRenderer renderer, Vector3 center, float radius)
    {
        if (!renderer) return;

        renderer.enabled = true;
        renderer.positionCount = CircleSegments;
        for (int i = 0; i < CircleSegments; i++)
        {
            float t = i / (float)CircleSegments * Mathf.PI * 2f;
            renderer.SetPosition(i, center + new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f) * radius);
        }
    }

    private static void DisableRenderer(LineRenderer renderer)
    {
        if (!renderer) return;
        renderer.positionCount = 0;
        renderer.enabled = false;
    }

    private void DisableExistingChildRenderer(string childName)
    {
        Transform child = transform.Find(childName);
        if (!child) return;

        LineRenderer renderer = child.GetComponent<LineRenderer>();
        DisableRenderer(renderer);
    }
}
