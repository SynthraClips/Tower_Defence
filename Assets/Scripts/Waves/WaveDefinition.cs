using UnityEngine;

[CreateAssetMenu(menuName = "Water TD/Waves/Wave Definition", fileName = "WaveDefinition")]
public class WaveDefinition : ScriptableObject
{
    [Header("Spawns")]
    public EnemySpawnEntry[] enemies;

    [Header("Flow")]
    [Min(0f)] public float intermissionOverride = -1f;
    public bool spawnBossAtEnd;

    [Header("Rewards")]
    public bool overrideWaveReward;
    [Min(0)] public int waveReward;
}
