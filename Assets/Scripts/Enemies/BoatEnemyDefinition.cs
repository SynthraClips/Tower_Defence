using UnityEngine;

[CreateAssetMenu(menuName = "Water TD/Enemies/Boat Definition", fileName = "BoatEnemyDefinition")]
public class BoatEnemyDefinition : ScriptableObject
{
    public string displayName = "Boat";

    [Header("Optional Visual Prefab")]
    [Tooltip("Optional prefab used for this boat definition. Leave empty to use the spawner's default boat prefab.")]
    public BoatEnemy prefabOverride;
    public float moveSpeed = 1.5f;
    public int maxHealth = 20;
    public int damageToBase = 1;
    public int goldReward = 5;
    [Min(0)] public int flatArmor = 0;
    [Min(0.1f)] public float siegeDamageMultiplier = 1f;
    [Min(0.1f)] public float arcaneDamageMultiplier = 1f;
    [Min(0.1f)] public float airburstDamageMultiplier = 1f;
    public Color tint = Color.white;
    public Vector3 scale = Vector3.one;
    public bool isFastWeakVariant;
}
