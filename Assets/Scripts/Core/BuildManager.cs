using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    [Header("Placement")]
    public bool requireBuildNodes = true;
    public LayerMask buildNodeLayer;
    public LayerMask placeableGround;   // e.g. "Ground"
    public LayerMask blockedLayers;     // e.g. "NoBuild" | "Enemy" | "Tower"
    public float   minClearRadius = 0.35f; // no overlap radius check
    public bool    snapToGrid = true;
    public float   gridSize = 0.5f;

    [Header("Ghost")]
    public Color okColor   = new Color(0.6f, 1f, 0.6f, 0.6f);
    public Color badColor  = new Color(1f, 0.5f, 0.5f, 0.6f);

    [HideInInspector] public Tower towerToBuild; // selected prefab

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TrySpend(int cost) => GameManager.Instance && GameManager.Instance.SpendGold(cost);

    public BuildNode GetBuildNodeAt(Vector3 position)
    {
        var hit = Physics2D.OverlapPoint(position, buildNodeLayer);
        return hit ? hit.GetComponent<BuildNode>() : null;
    }

    public static Vector3 Snap(Vector3 pos, float size)
    {
        pos.x = Mathf.Round(pos.x / size) * size;
        pos.y = Mathf.Round(pos.y / size) * size;
        pos.z = 0f;
        return pos;
    }
}
