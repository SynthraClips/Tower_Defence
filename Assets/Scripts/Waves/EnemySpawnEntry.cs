using System;
using UnityEngine;

[Serializable]
public struct EnemySpawnEntry
{
    public BoatEnemyDefinition definition;
    [Min(0)] public int count;
    [Min(0.05f)] public float spawnInterval;
}
