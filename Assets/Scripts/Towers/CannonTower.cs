using UnityEngine;

public class CannonTower : Tower
{
    private void Reset()
    {
        towerDisplayName = "Heavy Attack";
        towerArchetype = TowerArchetype.Heavy;
        damageType = DamageType.Siege;
        attackPreferenceMode = AttackPreferenceMode.Far;
        buildCost = 40;
        targetType = TowerTargetType.WaterOnly;
        range = 3.2f;
        shotsPerSecond = 0.8f;
    }
}
