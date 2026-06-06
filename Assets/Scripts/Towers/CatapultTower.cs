using UnityEngine;

public class CatapultTower : Tower
{
    private void Reset()
    {
        targetType = TowerTargetType.AirOnly; // flip to Any if you want dual-purpose
        range = 4.5f;
        shotsPerSecond = 0.7f;
    }
}
