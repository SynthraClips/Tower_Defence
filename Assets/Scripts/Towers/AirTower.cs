using UnityEngine;

public class AirTower : Tower
{
    private void Reset()
    {
        towerDisplayName = "Air Attack";
        towerArchetype = TowerArchetype.Air;
        targetType = TowerTargetType.AirOnly;
        range = 4.5f;
        shotsPerSecond = 1.25f;
        buildCost = 55;
    }
}
