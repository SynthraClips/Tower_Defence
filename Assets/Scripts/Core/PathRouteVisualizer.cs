using UnityEngine;

[ExecuteAlways]
public class PathRouteVisualizer : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Sprite waterSprite;
    [SerializeField] private Color routeColor = new Color(0.16f, 0.55f, 0.82f, 0.85f);
    [SerializeField] private Color bankColor = new Color(0.08f, 0.28f, 0.42f, 0.9f);
    [SerializeField] private Color foamColor = new Color(0.8f, 0.95f, 1f, 0.65f);
    [SerializeField] private Color coreOuterColor = new Color(0.1f, 0.45f, 0.75f, 0.95f);
    [SerializeField] private Color coreInnerColor = new Color(0.9f, 0.98f, 1f, 0.95f);
    [SerializeField] private float routeWidth = 1.1f;
    [SerializeField] private float bankWidth = 1.35f;
    [SerializeField] private float foamWidth = 0.28f;
    [SerializeField] private float coreRadiusOuter = 0.9f;
    [SerializeField] private float coreRadiusInner = 0.5f;
    [SerializeField] private int bankSortingOrder = -4;
    [SerializeField] private int routeSortingOrder = -3;
    [SerializeField] private int foamSortingOrder = -2;
    [SerializeField] private int coreSortingOrder = -1;

    private Path cachedPath;
    private LineRenderer routeRenderer;
    private LineRenderer bankRenderer;
    private LineRenderer foamRenderer;
    private LineRenderer coreOuterRenderer;
    private LineRenderer coreInnerRenderer;
    private const int CircleSegments = 40;

    private void Awake()
    {
        Rebuild();
    }

    private void OnEnable()
    {
        Rebuild();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Rebuild();
    }
#endif

    public void Rebuild()
    {
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
        {
            return;
        }

        cachedPath = cachedPath ? cachedPath : GetComponent<Path>();
        routeRenderer = routeRenderer ? routeRenderer : GetComponent<LineRenderer>();

        if (!cachedPath)
        {
            return;
        }

        EnsureVisualObjects();
        if (!routeRenderer || !bankRenderer || !foamRenderer || !coreOuterRenderer || !coreInnerRenderer)
        {
            return;
        }

        cachedPath.RebuildFromChildren();
        routeRenderer.positionCount = cachedPath.Count;
        bankRenderer.positionCount = cachedPath.Count;
        foamRenderer.positionCount = cachedPath.Count;
        for (int i = 0; i < cachedPath.Count; i++)
        {
            var waypoint = cachedPath.GetWaypoint(i);
            Vector3 position = waypoint ? waypoint.position : transform.position;
            routeRenderer.SetPosition(i, position);
            bankRenderer.SetPosition(i, position);
            foamRenderer.SetPosition(i, position);
        }

        if (cachedPath.Count > 0)
        {
            Vector3 endPoint = cachedPath.GetWaypoint(cachedPath.Count - 1).position;
            RebuildCircle(coreOuterRenderer, endPoint, coreRadiusOuter);
            RebuildCircle(coreInnerRenderer, endPoint, coreRadiusInner);
        }
    }

    private void EnsureVisualObjects()
    {
        routeRenderer = routeRenderer ? routeRenderer : GetComponent<LineRenderer>();
        bankRenderer = bankRenderer ? bankRenderer : GetOrCreateChildRenderer("RouteBank");
        foamRenderer = foamRenderer ? foamRenderer : GetOrCreateChildRenderer("RouteFoam");
        coreOuterRenderer = coreOuterRenderer ? coreOuterRenderer : GetOrCreateChildRenderer("HarborCoreOuter");
        coreInnerRenderer = coreInnerRenderer ? coreInnerRenderer : GetOrCreateChildRenderer("HarborCoreInner");

        ConfigurePathRenderer(bankRenderer, bankWidth, bankColor, bankSortingOrder, false);
        ConfigurePathRenderer(routeRenderer, routeWidth, routeColor, routeSortingOrder, true);
        ConfigurePathRenderer(foamRenderer, foamWidth, foamColor, foamSortingOrder, false);
        ConfigureCircleRenderer(coreOuterRenderer, coreOuterColor, coreSortingOrder);
        ConfigureCircleRenderer(coreInnerRenderer, coreInnerColor, coreSortingOrder + 1);
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

        return renderer;
    }

    private void ConfigurePathRenderer(LineRenderer renderer, float width, Color color, int order, bool textured)
    {
        if (!renderer) return;

        renderer.loop = false;
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
        renderer.textureMode = textured ? LineTextureMode.Tile : LineTextureMode.Stretch;

        if (renderer.sharedMaterial == null && Shader.Find("Sprites/Default") != null)
        {
            renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        if (textured && renderer.sharedMaterial != null && waterSprite != null)
        {
            renderer.sharedMaterial.mainTexture = waterSprite.texture;
        }
    }

    private void ConfigureCircleRenderer(LineRenderer renderer, Color color, int order)
    {
        if (!renderer) return;

        renderer.loop = true;
        renderer.useWorldSpace = true;
        renderer.numCornerVertices = 4;
        renderer.numCapVertices = 4;
        renderer.startWidth = 0.12f;
        renderer.endWidth = 0.12f;
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

    private void RebuildCircle(LineRenderer renderer, Vector3 center, float radius)
    {
        if (!renderer) return;

        renderer.positionCount = CircleSegments;
        for (int i = 0; i < CircleSegments; i++)
        {
            float t = i / (float)CircleSegments * Mathf.PI * 2f;
            renderer.SetPosition(i, center + new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0f) * radius);
        }
    }
}
