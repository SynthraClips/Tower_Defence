using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Path))]
public class PathRouteVisualizer : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Color routeColor = new Color(0.16f, 0.55f, 0.82f, 0.85f);
    [SerializeField] private float routeWidth = 0.9f;
    [SerializeField] private int sortingOrder = -2;

    private Path cachedPath;
    private LineRenderer routeRenderer;

    private void Awake()
    {
        EnsureRenderer();
        Rebuild();
    }

    private void OnEnable()
    {
        EnsureRenderer();
        Rebuild();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureRenderer();
        Rebuild();
    }
#endif

    public void Rebuild()
    {
        cachedPath = cachedPath ? cachedPath : GetComponent<Path>();
        if (!cachedPath)
        {
            return;
        }

        cachedPath.RebuildFromChildren();
        if (routeRenderer == null)
        {
            EnsureRenderer();
        }

        routeRenderer.positionCount = cachedPath.Count;
        for (int i = 0; i < cachedPath.Count; i++)
        {
            var waypoint = cachedPath.GetWaypoint(i);
            routeRenderer.SetPosition(i, waypoint ? waypoint.position : transform.position);
        }
    }

    private void EnsureRenderer()
    {
        cachedPath = cachedPath ? cachedPath : GetComponent<Path>();
        routeRenderer = GetComponent<LineRenderer>();
        if (!routeRenderer)
        {
            routeRenderer = gameObject.AddComponent<LineRenderer>();
        }

        routeRenderer.loop = false;
        routeRenderer.useWorldSpace = true;
        routeRenderer.numCornerVertices = 4;
        routeRenderer.numCapVertices = 4;
        routeRenderer.startWidth = routeWidth;
        routeRenderer.endWidth = routeWidth;
        routeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        routeRenderer.receiveShadows = false;
        routeRenderer.sortingOrder = sortingOrder;
        routeRenderer.startColor = routeColor;
        routeRenderer.endColor = routeColor;

        if (routeRenderer.sharedMaterial == null)
        {
            routeRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }
    }
}
