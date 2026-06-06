using UnityEngine;

public class BallistaTower : Tower
{
    private void Reset()
    {
        targetType = TowerTargetType.WaterOnly;
        range = 4.0f;
        shotsPerSecond = 1.5f;
		
    }
}
