using UnityEngine;

public class BallistaTower : Tower
{
    private void Reset()
    {
        towerDisplayName = "Light Attack";
        towerArchetype = TowerArchetype.Light;
        damageType = DamageType.Physical;
        attackPreferenceMode = AttackPreferenceMode.Close;
        buildCost = 30;
        targetType = TowerTargetType.WaterOnly;
        range = 4.0f;
        shotsPerSecond = 1.5f;
    }
}
