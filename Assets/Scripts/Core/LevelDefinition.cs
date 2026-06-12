using UnityEngine;

[CreateAssetMenu(menuName = "Water TD/Levels/Level Definition", fileName = "LevelDefinition")]
public class LevelDefinition : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "New Level";
    public string sceneName = string.Empty;

    [Header("Placement")]
    public bool useManualPlacementBounds = true;
    public Vector2 placementBoundsCenter = Vector2.zero;
    public Vector2 placementBoundsSize = new Vector2(18f, 12f);
    [Min(0.1f)] public float waterRouteWidth = 1.55f;

    [Header("Waves")]
    public WaveDefinition[] waveDefinitions;
}
