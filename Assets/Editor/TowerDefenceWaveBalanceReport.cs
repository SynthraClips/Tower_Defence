#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TowerDefenceWaveBalanceReport
{
    private static readonly string[] GameplayScenes =
    {
        "Assets/Scenes/Easy Level.unity",
        "Assets/Scenes/MediumLevel.unity",
        "Assets/Scenes/HardLevel.unity",
    };

    [MenuItem("Tools/Tower Defence/Wave Balance Report")]
    public static void RunReport()
    {
        Debug.Log("[WaveBalance] Starting wave balance report...");

        foreach (string scenePath in GameplayScenes)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"[WaveBalance] Missing scene: {scenePath}");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                Main main = FindInScene<Main>(scene);
                Spawner spawner = FindInScene<Spawner>(scene);
                if (!main || !spawner)
                {
                    Debug.LogWarning($"[WaveBalance] Scene is missing Main or Spawner: {scenePath}");
                    continue;
                }

                Debug.Log($"[WaveBalance] Scene: {scene.name}");
                if (main.waveDefinitions != null && main.waveDefinitions.Length > 0)
                {
                    ReportDefinitionWaves(scene.name, spawner, main.waveDefinitions);
                }
                else if (main.waves != null && main.waves.Length > 0)
                {
                    ReportLegacyWaves(scene.name, spawner, main.waves);
                }
                else
                {
                    Debug.LogWarning($"[WaveBalance] No waves configured in scene: {scene.name}");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        Debug.Log("[WaveBalance] Report complete.");
    }

    private static void ReportDefinitionWaves(string sceneName, Spawner spawner, WaveDefinition[] waveDefinitions)
    {
        float previousWaveHealth = 0f;
        for (int i = 0; i < waveDefinitions.Length; i++)
        {
            WaveDefinition wave = waveDefinitions[i];
            if (!wave)
            {
                Debug.LogError($"[WaveBalance] {sceneName} Wave {i + 1}: missing WaveDefinition asset.");
                continue;
            }

            int enemyCount = 0;
            float healthTotal = 0f;
            int enemyGoldTotal = 0;
            float spawnDuration = 0f;
            var issues = new List<string>();

            if (wave.enemies == null || wave.enemies.Length == 0)
            {
                issues.Add("zero enemies");
            }
            else
            {
                foreach (EnemySpawnEntry entry in wave.enemies)
                {
                    if (!entry.definition)
                    {
                        issues.Add("missing enemy definition");
                        continue;
                    }

                    if (entry.count <= 0)
                    {
                        issues.Add("entry count <= 0");
                        continue;
                    }

                    enemyCount += entry.count;
                    healthTotal += entry.count * Mathf.Max(0, entry.definition.maxHealth);
                    enemyGoldTotal += entry.count * Mathf.Max(0, entry.definition.goldReward);
                    spawnDuration += entry.count * Mathf.Max(0.05f, entry.spawnInterval);

                    if (entry.definition.maxHealth <= 0)
                    {
                        issues.Add($"{entry.definition.name} health <= 0");
                    }

                    if (entry.definition.goldReward < 0)
                    {
                        issues.Add($"{entry.definition.name} reward < 0");
                    }
                }
            }

            bool includesBoss = wave.spawnBossAtEnd || (spawner.enableBossWaves && spawner.bossEveryNWaves > 0 && (i + 1) % spawner.bossEveryNWaves == 0);
            float bossHealth = includesBoss ? ResolveBossHealth(spawner) : 0f;
            int bossReward = includesBoss ? ResolveBossReward(spawner) : 0;
            float totalHealth = healthTotal + bossHealth;
            int waveReward = wave.overrideWaveReward ? Mathf.Max(0, wave.waveReward) : spawner.baseWaveGold + spawner.perWaveBonus * (i + 1);
            int totalGold = enemyGoldTotal + waveReward + bossReward + (includesBoss ? Mathf.Max(0, spawner.bossWaveBonusGold) : 0);
            float intermission = wave.intermissionOverride >= 0f ? wave.intermissionOverride : spawner.timeBetweenWaves;

            if (enemyCount == 0)
            {
                issues.Add("zero enemies");
            }

            if (wave.overrideWaveReward && wave.waveReward == 0)
            {
                issues.Add("wave reward override is zero");
            }

            if (previousWaveHealth > 0f && totalHealth > previousWaveHealth * 2.5f)
            {
                issues.Add("large health spike vs previous wave");
            }

            Debug.Log(
                $"[WaveBalance] {sceneName} Wave {i + 1} ({wave.name}): " +
                $"enemies={enemyCount}, totalHealth~={totalHealth:0}, totalGold~={totalGold}, " +
                $"boss={(includesBoss ? "yes" : "no")}, spawnTime~={spawnDuration:0.0}s, intermission={intermission:0.0}s" +
                $"{FormatIssues(issues)}");

            previousWaveHealth = Mathf.Max(1f, totalHealth);
        }
    }

    private static void ReportLegacyWaves(string sceneName, Spawner spawner, Spawner.Wave[] waves)
    {
        float previousWaveHealth = 0f;
        for (int i = 0; i < waves.Length; i++)
        {
            Spawner.Wave wave = waves[i];
            int weakCount = Mathf.Max(0, wave.tier1);
            int mediumCount = Mathf.Max(0, wave.tier2);
            int hardCount = Mathf.Max(0, wave.tier3);
            int enemyCount = weakCount + mediumCount + hardCount;

            float totalHealth =
                weakCount * ResolveDefinitionHealth(spawner.weakBoatDefinition, spawner.boatPrefab) +
                mediumCount * ResolveDefinitionHealth(spawner.mediumBoatDefinition, spawner.boatPrefab) +
                hardCount * ResolveDefinitionHealth(spawner.hardBoatDefinition, spawner.boatPrefab);

            int enemyGold =
                weakCount * ResolveDefinitionReward(spawner.weakBoatDefinition, spawner.boatPrefab) +
                mediumCount * ResolveDefinitionReward(spawner.mediumBoatDefinition, spawner.boatPrefab) +
                hardCount * ResolveDefinitionReward(spawner.hardBoatDefinition, spawner.boatPrefab);

            bool includesBoss = spawner.enableBossWaves && spawner.bossEveryNWaves > 0 && (i + 1) % spawner.bossEveryNWaves == 0;
            if (includesBoss)
            {
                totalHealth += ResolveBossHealth(spawner);
                enemyGold += ResolveBossReward(spawner);
            }

            int waveReward = spawner.baseWaveGold + spawner.perWaveBonus * (i + 1) + (includesBoss ? Mathf.Max(0, spawner.bossWaveBonusGold) : 0);
            float spawnDuration = enemyCount * Mathf.Max(0.05f, wave.spawnInterval);
            var issues = new List<string>();

            if (enemyCount == 0)
            {
                issues.Add("zero enemies");
            }

            if (previousWaveHealth > 0f && totalHealth > previousWaveHealth * 2.5f)
            {
                issues.Add("large health spike vs previous wave");
            }

            Debug.Log(
                $"[WaveBalance] {sceneName} Wave {i + 1} (legacy): " +
                $"enemies={enemyCount}, totalHealth~={totalHealth:0}, totalGold~={enemyGold + waveReward}, " +
                $"boss={(includesBoss ? "yes" : "no")}, spawnTime~={spawnDuration:0.0}s, intermission={spawner.timeBetweenWaves:0.0}s" +
                $"{FormatIssues(issues)}");

            previousWaveHealth = Mathf.Max(1f, totalHealth);
        }
    }

    private static float ResolveBossHealth(Spawner spawner)
    {
        return spawner.bossBoatDefinition ? Mathf.Max(1, spawner.bossBoatDefinition.maxHealth) : Mathf.Max(1, spawner.bossHealth);
    }

    private static int ResolveBossReward(Spawner spawner)
    {
        return spawner.bossBoatDefinition ? Mathf.Max(0, spawner.bossBoatDefinition.goldReward) : Mathf.Max(0, spawner.bossGoldReward);
    }

    private static int ResolveDefinitionReward(BoatEnemyDefinition definition, BoatEnemy fallbackPrefab)
    {
        return definition ? Mathf.Max(0, definition.goldReward) : fallbackPrefab ? Mathf.Max(0, fallbackPrefab.goldReward) : 0;
    }

    private static float ResolveDefinitionHealth(BoatEnemyDefinition definition, BoatEnemy fallbackPrefab)
    {
        return definition ? Mathf.Max(1, definition.maxHealth) : fallbackPrefab ? Mathf.Max(1, fallbackPrefab.maxHealth) : 1f;
    }

    private static string FormatIssues(List<string> issues)
    {
        return issues.Count == 0 ? string.Empty : $" | flags: {string.Join(", ", issues)}";
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            T component = rootObject.GetComponentInChildren<T>(true);
            if (component)
            {
                return component;
            }
        }

        return null;
    }
}
#endif
