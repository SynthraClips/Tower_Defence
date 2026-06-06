using UnityEngine;

public class MagicTower : Tower
{
    private void Reset()
    {
        towerDisplayName = "Magic Attack";
        towerArchetype = TowerArchetype.Magic;
        targetType = TowerTargetType.WaterOnly;
        range = 3.8f;
        shotsPerSecond = 1.1f;
        buildCost = 45;
    }
}
