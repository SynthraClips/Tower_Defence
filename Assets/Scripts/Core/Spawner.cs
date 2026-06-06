using System;
using System.Collections;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Serializable]
    public struct Wave
    {
        public int tier1;
        public int tier2;
        public int tier3;
        public float spawnInterval;
    }

    [Header("References")]
    public Path path;
    public BoatEnemy boatPrefab;
    public WaveDefinition[] waveDefinitions;

    [Header("Timing")]
    public float timeBetweenWaves = 6f;

    [Header("Wave Rewards")]
    public int baseWaveGold = 20;
    public int perWaveBonus = 2;

    [Header("Boss Settings")]
    public bool enableBossWaves = true;
    [Min(2)] public int bossEveryNWaves = 5;
    [Tooltip("Optional: specific boss prefab. If null, the standard boat prefab will be reused.")]
    public BoatEnemy bossPrefab;
    public int bossHealth = 600;
    public float bossMoveSpeed = 0.9f;
    public int bossDamageToBase = 5;
    public int bossGoldReward = 120;

    [Header("Boss Rewards")]
    public int bossWaveBonusGold = 50;
    public int bossLifeHeal = 1;

    public int CurrentWaveIndex { get; private set; }
    public int TotalWaves { get; private set; }

    public event Action<int, int> OnWaveStarted;
    public event Action<float> OnIntermissionStarted;
    public event Action<float> OnIntermissionTick;
    public event Action OnIntermissionEnded;
    public event Action<int, int> OnWaveCompleted;
    public event Action<int> OnBossSpawned;

    private int aliveThisWave;
    private Coroutine waveRoutine;
    private Wave[] legacyWaves;

    private void OnEnable()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnGameOver += HandleRunEnded;
            GameManager.Instance.OnVictory += HandleRunEnded;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnGameOver -= HandleRunEnded;
            GameManager.Instance.OnVictory -= HandleRunEnded;
        }
    }

    public void Begin(Wave[] waves)
    {
        legacyWaves = waves;
        TotalWaves = waves?.Length ?? 0;
        RestartWaveRoutine(RunLegacyWaves());
    }

    public void Begin(WaveDefinition[] waves)
    {
        waveDefinitions = waves;
        TotalWaves = waves?.Length ?? 0;
        RestartWaveRoutine(RunWaveDefinitions());
    }

    public void OnEnemyRemoved(Enemy enemy)
    {
        aliveThisWave = Mathf.Max(0, aliveThisWave - 1);
    }

    private void HandleRunEnded()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
    }

    private void RestartWaveRoutine(IEnumerator routine)
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
        }

        waveRoutine = StartCoroutine(routine);
    }

    private IEnumerator RunLegacyWaves()
    {
        if (legacyWaves == null || legacyWaves.Length == 0)
        {
            Debug.LogWarning("[Spawner] No legacy waves configured.");
            yield break;
        }

        yield return RunWaveLoop(
            legacyWaves.Length,
            index => SpawnLegacyWave(legacyWaves[index]),
            index => timeBetweenWaves,
            index => false,
            index => false,
            index => 0);
    }

    private IEnumerator RunWaveDefinitions()
    {
        if (waveDefinitions == null || waveDefinitions.Length == 0)
        {
            Debug.LogWarning("[Spawner] No wave definitions configured.");
            yield break;
        }

        yield return RunWaveLoop(
            waveDefinitions.Length,
            index => SpawnWaveDefinition(waveDefinitions[index]),
            index => waveDefinitions[index] && waveDefinitions[index].intermissionOverride >= 0f
                ? waveDefinitions[index].intermissionOverride
                : timeBetweenWaves,
            index => waveDefinitions[index] && waveDefinitions[index].spawnBossAtEnd,
            index => waveDefinitions[index] && waveDefinitions[index].overrideWaveReward,
            index => waveDefinitions[index] ? waveDefinitions[index].waveReward : 0);
    }

    private IEnumerator RunWaveLoop(
        int waveCount,
        Func<int, IEnumerator> spawnRoutineFactory,
        Func<int, float> intermissionResolver,
        Func<int, bool> bossOverrideResolver,
        Func<int, bool> rewardOverrideEnabledResolver,
        Func<int, int> rewardOverrideValueResolver)
    {
        if (!ValidatePath())
        {
            yield break;
        }

        GameManager.Instance?.StartRun(waveCount);
        yield return new WaitForSeconds(1f);

        for (int waveIndex = 0; waveIndex < waveCount; waveIndex++)
        {
            CurrentWaveIndex = waveIndex + 1;
            aliveThisWave = 0;

            GameManager.Instance?.SetCurrentWave(CurrentWaveIndex);
            OnWaveStarted?.Invoke(CurrentWaveIndex, TotalWaves);

            bool isBossWave =
                bossOverrideResolver(waveIndex) ||
                (enableBossWaves && bossEveryNWaves > 0 && CurrentWaveIndex % bossEveryNWaves == 0);

            yield return spawnRoutineFactory(waveIndex);

            if (isBossWave)
            {
                SpawnBoss();
            }

            yield return new WaitUntil(() =>
                aliveThisWave == 0 || (GameManager.Instance && GameManager.Instance.IsGameOver));

            if (GameManager.Instance && GameManager.Instance.IsGameOver)
            {
                yield break;
            }

            int reward = rewardOverrideEnabledResolver(waveIndex)
                ? Mathf.Max(0, rewardOverrideValueResolver(waveIndex))
                : baseWaveGold + perWaveBonus * CurrentWaveIndex;

            if (isBossWave)
            {
                reward += Mathf.Max(0, bossWaveBonusGold);
                if (bossLifeHeal > 0)
                {
                    GameManager.Instance?.HealLife(bossLifeHeal);
                }
            }

            GameManager.Instance?.AddGold(reward);
            OnWaveCompleted?.Invoke(CurrentWaveIndex, reward);

            if (waveIndex < waveCount - 1)
            {
                yield return RunIntermission(intermissionResolver(waveIndex));
            }
        }

        waveRoutine = null;
        GameManager.Instance?.TriggerVictory();
    }

    private IEnumerator SpawnLegacyWave(Wave wave)
    {
        for (int i = 0; i < wave.tier1; i++)
        {
            Spawn(BoatTier.Skiff);
            yield return new WaitForSeconds(Mathf.Max(0.05f, wave.spawnInterval));
        }

        for (int i = 0; i < wave.tier2; i++)
        {
            Spawn(BoatTier.Cutter);
            yield return new WaitForSeconds(Mathf.Max(0.05f, wave.spawnInterval));
        }

        for (int i = 0; i < wave.tier3; i++)
        {
            Spawn(BoatTier.Frigate);
            yield return new WaitForSeconds(Mathf.Max(0.05f, wave.spawnInterval));
        }
    }

    private IEnumerator SpawnWaveDefinition(WaveDefinition waveDefinition)
    {
        if (waveDefinition == null || waveDefinition.enemies == null)
        {
            yield break;
        }

        foreach (var entry in waveDefinition.enemies)
        {
            int count = Mathf.Max(0, entry.count);
            float interval = Mathf.Max(0.05f, entry.spawnInterval);
            for (int i = 0; i < count; i++)
            {
                Spawn(entry.tier);
                yield return new WaitForSeconds(interval);
            }
        }
    }

    private IEnumerator RunIntermission(float duration)
    {
        if (duration <= 0f)
        {
            yield break;
        }

        float remaining = duration;
        OnIntermissionStarted?.Invoke(duration);
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            OnIntermissionTick?.Invoke(Mathf.Max(0f, remaining));
            yield return null;
        }

        OnIntermissionEnded?.Invoke();
    }

    private void Spawn(BoatTier tier)
    {
        if (!ValidatePath() || !boatPrefab)
        {
            Debug.LogWarning("[Spawner] Missing path or boat prefab, spawn skipped.");
            return;
        }

        var boat = Instantiate(boatPrefab, path.GetWaypoint(0).position, Quaternion.identity);
        boat.path = path;
        boat.ownerSpawner = this;
        boat.SetTier(tier);
        aliveThisWave++;
    }

    private void SpawnBoss()
    {
        if (!ValidatePath())
        {
            return;
        }

        var prefab = bossPrefab ? bossPrefab : boatPrefab;
        if (!prefab)
        {
            Debug.LogWarning("[Spawner] Boss spawn requested but no boss-capable prefab is assigned.");
            return;
        }

        var boss = Instantiate(prefab, path.GetWaypoint(0).position, Quaternion.identity);
        boss.path = path;
        boss.ownerSpawner = this;
        boss.maxHealth = Mathf.Max(1, bossHealth);
        boss.moveSpeed = Mathf.Max(0.01f, bossMoveSpeed);
        boss.damageToBase = Mathf.Max(1, bossDamageToBase);
        boss.goldReward = Mathf.Max(0, bossGoldReward);
        boss.ForceSetCurrentHealth(boss.maxHealth);

        aliveThisWave++;
        OnBossSpawned?.Invoke(CurrentWaveIndex);
    }

    private bool ValidatePath()
    {
        if (path != null && path.Count > 0)
        {
            return true;
        }

        Debug.LogWarning("[Spawner] Path is missing or has no waypoints.");
        return false;
    }
}
