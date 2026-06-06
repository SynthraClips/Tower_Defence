using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BuildNode : MonoBehaviour
{
    [Header("Rules")]
    public bool isBuildable = true;
    public bool occupyOnStart;

    [Header("Optional Visuals")]
    [SerializeField] private SpriteRenderer highlightRenderer;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color validColor = new Color(0.5f, 1f, 0.7f, 1f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.45f, 0.45f, 1f);

    public Tower OccupyingTower { get; private set; }
    public bool IsOccupied => OccupyingTower != null || occupyOnStart;
    public bool CanBuild => isBuildable && !IsOccupied;

    private void Awake()
    {
        SetHighlightState(null);
    }

    public bool TryOccupy(Tower tower)
    {
        if (!tower || !CanBuild)
        {
            return false;
        }

        OccupyingTower = tower;
        tower.AssignBuildNode(this);
        SetHighlightState(null);
        return true;
    }

    public void ClearOccupancy(Tower tower)
    {
        if (tower != null && OccupyingTower != tower)
        {
            return;
        }

        OccupyingTower = null;
        SetHighlightState(null);
    }

    public void SetHighlightState(bool? validPlacement)
    {
        if (!highlightRenderer)
        {
            return;
        }

        highlightRenderer.color = validPlacement switch
        {
            true => validColor,
            false => invalidColor,
            _ => idleColor,
        };
    }
}
