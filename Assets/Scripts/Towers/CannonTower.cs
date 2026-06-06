using UnityEngine;

public class CannonTower : Tower

{
    private void Reset()
    {
        targetType = TowerTargetType.WaterOnly;
        range = 3.2f;
        shotsPerSecond = 0.8f;
    }
}
