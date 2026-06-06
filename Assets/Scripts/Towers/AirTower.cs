using UnityEngine;

public class AirTower : Tower
{
    private void Reset()
    {
        towerDisplayName = "Air Attack";
        towerArchetype = TowerArchetype.Air;
        damageType = DamageType.Airburst;
        attackPreferenceMode = AttackPreferenceMode.Far;
        targetType = TowerTargetType.AirOnly;
        range = 4.5f;
        shotsPerSecond = 1.25f;
        buildCost = 55;
    }
}
