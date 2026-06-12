#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WaterTD;

public static class TowerDefenceSetupValidator
{
    private static readonly string[] RequiredEnabledScenePaths =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/LevelSelect.unity",
        "Assets/Scenes/Settings.unity",
        "Assets/Scenes/Easy Level.unity",
        "Assets/Scenes/MediumLevel.unity",
        "Assets/Scenes/HardLevel.unity",
    };

    [MenuItem("Tools/Tower Defence/Validate Game Setup")]
    public static void ValidateGameSetup()
    {
        var report = new ValidationReport();

        ValidateBuildSettings(report);
        ValidateCorePrefabs(report);
        ValidateGameplayScenes(report);
        ValidateEnemyPrefabs(report);
        ValidateTowerPrefabs(report);
        ValidateProjectilePrefabs(report);
        ValidateWaveDefinitions(report);
        ValidateBoatDefinitions(report);

        report.LogSummary();
    }

    private static void ValidateBuildSettings(ValidationReport report)
    {
        var enabledSceneLookup = new Dictionary<string, bool>();
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            enabledSceneLookup[buildScene.path] = buildScene.enabled;
        }

        foreach (string requiredScenePath in RequiredEnabledScenePaths)
        {
            if (!System.IO.File.Exists(requiredScenePath))
            {
                report.Error($"Missing required scene asset: {requiredScenePath}");
                continue;
            }

            if (!enabledSceneLookup.TryGetValue(requiredScenePath, out bool enabled))
            {
                report.Error($"Scene is missing from Build Settings: {requiredScenePath}");
                continue;
            }

            if (!enabled)
            {
                report.Error($"Scene is present but disabled in Build Settings: {requiredScenePath}");
            }
        }
    }

    private static void ValidateCorePrefabs(ValidationReport report)
    {
        ValidatePrefabComponent<AudioManager>("Assets/Prefabs/Core/AudioManager.prefab", report);
        ValidatePrefabComponent<GameManager>("Assets/Prefabs/Core/GameManager.prefab", report);
        ValidatePrefabComponent<Main>("Assets/Prefabs/Core/Main.prefab", report);
        ValidatePrefabComponent<Spawner>("Assets/Prefabs/Core/Spawner.prefab", report);
        ValidatePrefabAssetExists("Assets/Prefabs/Core/HUDUI.prefab", report);
        ValidatePrefabAssetExists("Assets/Prefabs/Core/TowerShopUI.prefab", report);

        AudioManager audioManagerPrefab = LoadPrefabComponent<AudioManager>("Assets/Prefabs/Core/AudioManager.prefab", report);
        if (audioManagerPrefab != null)
        {
            if (!audioManagerPrefab.masterMixer)
            {
                report.Warning("AudioManager prefab is missing a master mixer reference.", audioManagerPrefab);
            }

            if (!audioManagerPrefab.musicGroup)
            {
                report.Warning("AudioManager prefab is missing a music mixer group reference.", audioManagerPrefab);
            }

            if (!audioManagerPrefab.sfxGroup)
            {
                report.Warning("AudioManager prefab is missing an SFX mixer group reference.", audioManagerPrefab);
            }

            if (audioManagerPrefab.sfxLibrary == null || audioManagerPrefab.sfxLibrary.Count == 0)
            {
                report.Warning("AudioManager prefab has an empty SFX library.", audioManagerPrefab);
            }

            foreach (AudioManager.MusicCue cue in System.Enum.GetValues(typeof(AudioManager.MusicCue)))
            {
                if (!AudioManagerHasConfiguredMusic(audioManagerPrefab, cue))
                {
                    report.Warning($"AudioManager is missing a configured music section or fallback clip for cue '{cue}'.", audioManagerPrefab);
                }
            }
        }

        Spawner spawnerPrefab = LoadPrefabComponent<Spawner>("Assets/Prefabs/Core/Spawner.prefab", report);
        if (spawnerPrefab != null && !spawnerPrefab.boatPrefab)
        {
            report.Warning("Spawner prefab has no default boat prefab assigned.", spawnerPrefab);
        }
    }

    private static void ValidateGameplayScenes(ValidationReport report)
    {
        string[] gameplayScenes =
        {
            "Assets/Scenes/Easy Level.unity",
            "Assets/Scenes/MediumLevel.unity",
            "Assets/Scenes/HardLevel.unity",
        };

        foreach (string scenePath in gameplayScenes)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                Main main = FindInScene<Main>(scene);
                BuildManager buildManager = FindInScene<BuildManager>(scene);
                TowerPlacer towerPlacer = FindInScene<TowerPlacer>(scene);
                Spawner spawner = FindInScene<Spawner>(scene);
                global::Path path = FindInScene<global::Path>(scene);
                HUDController hud = FindInScene<HUDController>(scene);
                GameManager sceneGameManager = FindInScene<GameManager>(scene);

                if (!main)
                {
                    report.Error($"Gameplay scene is missing Main: {scenePath}");
                }

                if (!sceneGameManager && !main)
                {
                    report.Error($"Gameplay scene has no GameManager and no Main bootstrap: {scenePath}");
                }
                else if (!sceneGameManager)
                {
                    report.Warning($"Gameplay scene relies on Main to bootstrap GameManager at runtime: {scenePath}");
                }

                if (!buildManager)
                {
                    report.Error($"Gameplay scene is missing BuildManager: {scenePath}");
                }

                if (!towerPlacer)
                {
                    report.Error($"Gameplay scene is missing TowerPlacer: {scenePath}");
                }

                if (!spawner)
                {
                    report.Error($"Gameplay scene is missing Spawner: {scenePath}");
                }

                if (!path)
                {
                    report.Error($"Gameplay scene is missing Path: {scenePath}");
                }

                if (!hud)
                {
                    report.Warning($"Gameplay scene is missing HUDController: {scenePath}");
                }

                TowerShopUI shop = FindInScene<TowerShopUI>(scene);
                if (!shop)
                {
                    report.Warning($"Gameplay scene is missing TowerShopUI: {scenePath}");
                }

                ValidatePath(scenePath, path, report);
                ValidateSpawner(scenePath, spawner, report);
                ValidateMain(scenePath, main, report);
                ValidateBuildNodes(scenePath, buildManager, path, report);
                ValidateHud(scenePath, hud, report);
                ValidateTowerShop(scenePath, shop, report);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void ValidateEnemyPrefabs(ValidationReport report)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (!prefab)
            {
                continue;
            }

            Enemy enemy = prefab.GetComponent<Enemy>();
            if (!enemy)
            {
                report.Warning($"Enemy prefab is missing Enemy component: {assetPath}", prefab);
                continue;
            }

            if (!prefab.GetComponentInChildren<Collider2D>(true))
            {
                report.Error($"Enemy prefab is missing Collider2D: {assetPath}", prefab);
            }

            if (!prefab.GetComponent<Rigidbody2D>())
            {
                report.Error($"Enemy prefab is missing Rigidbody2D: {assetPath}", prefab);
            }

            SerializedObject enemyObject = new SerializedObject(enemy);
            if (enemyObject.FindProperty("alwaysShowHealthBar")?.boolValue == true)
            {
                if (enemyObject.FindProperty("healthBarPrefab")?.objectReferenceValue == null)
                {
                    report.Warning($"Enemy prefab relies on runtime-created health bars instead of an assigned health bar prefab: {assetPath}", prefab);
                }

                if (enemyObject.FindProperty("healthBarFillSprite")?.objectReferenceValue == null)
                {
                    report.Warning($"Enemy prefab is missing a health bar fill sprite: {assetPath}", prefab);
                }

                if (enemyObject.FindProperty("healthBarBackgroundSprite")?.objectReferenceValue == null)
                {
                    report.Warning($"Enemy prefab is missing a health bar background sprite: {assetPath}", prefab);
                }
            }

            bool hasExplosionPrefab = enemyObject.FindProperty("deathExplosionPrefab")?.objectReferenceValue != null;
            bool hasExplosionSprite = enemyObject.FindProperty("deathExplosionSprite")?.objectReferenceValue != null;
            if (!hasExplosionPrefab && !hasExplosionSprite)
            {
                report.Warning($"Enemy prefab is missing both explosion prefab and explosion sprite setup: {assetPath}", prefab);
            }
        }
    }

    private static void ValidateTowerPrefabs(ValidationReport report)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Towers" }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Tower tower = prefab ? prefab.GetComponent<Tower>() : null;
            if (!tower)
            {
                report.Warning($"Tower prefab is missing Tower component: {assetPath}", prefab);
                continue;
            }

            if (tower.range <= 0f)
            {
                report.Error($"Tower has invalid range <= 0: {assetPath}", prefab);
            }

            if (tower.shotsPerSecond <= 0f)
            {
                report.Error($"Tower has invalid shotsPerSecond <= 0: {assetPath}", prefab);
            }

            if (tower.enemyLayer.value == 0)
            {
                report.Warning($"Tower enemyLayer mask is empty: {assetPath}", prefab);
            }

            if (!tower.projectilePrefab)
            {
                report.Error($"Tower is missing projectile prefab: {assetPath}", prefab);
            }

            if (!tower.rotatingPart)
            {
                report.Warning($"Tower is missing rotatingPart reference: {assetPath}", prefab);
            }

            if (!tower.firePoint)
            {
                report.Warning($"Tower is missing firePoint reference. It will fall back to the tower transform: {assetPath}", prefab);
            }

            if (!tower.shopIcon)
            {
                report.Warning($"Tower is missing shop icon: {assetPath}", prefab);
            }
        }
    }

    private static void ValidateProjectilePrefabs(ValidationReport report)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Projectiles" }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Projectile projectile = prefab ? prefab.GetComponent<Projectile>() : null;
            if (!projectile)
            {
                report.Warning($"Projectile prefab is missing Projectile component: {assetPath}", prefab);
                continue;
            }

            if (projectile.speed <= 0f)
            {
                report.Error($"Projectile speed must be > 0: {assetPath}", prefab);
            }

            if (projectile.damage <= 0)
            {
                report.Warning($"Projectile damage is <= 0: {assetPath}", prefab);
            }

            if (!prefab.GetComponent<Collider2D>())
            {
                report.Error($"Projectile prefab is missing Collider2D: {assetPath}", prefab);
            }

            if (!prefab.GetComponent<Rigidbody2D>())
            {
                report.Error($"Projectile prefab is missing Rigidbody2D: {assetPath}", prefab);
            }
        }
    }

    private static void ValidateWaveDefinitions(ValidationReport report)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:WaveDefinition"))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            WaveDefinition waveDefinition = AssetDatabase.LoadAssetAtPath<WaveDefinition>(assetPath);
            if (!waveDefinition)
            {
                continue;
            }

            if (waveDefinition.enemies == null || waveDefinition.enemies.Length == 0)
            {
                report.Warning($"WaveDefinition has no enemy entries: {assetPath}", waveDefinition);
                continue;
            }

            for (int i = 0; i < waveDefinition.enemies.Length; i++)
            {
                EnemySpawnEntry entry = waveDefinition.enemies[i];
                if (!entry.definition)
                {
                    report.Error($"WaveDefinition has a missing enemy definition at index {i}: {assetPath}", waveDefinition);
                }

                if (entry.count <= 0)
                {
                    report.Warning($"WaveDefinition entry has count <= 0 at index {i}: {assetPath}", waveDefinition);
                }
            }
        }
    }

    private static void ValidateBoatDefinitions(ValidationReport report)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:BoatEnemyDefinition"))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            BoatEnemyDefinition definition = AssetDatabase.LoadAssetAtPath<BoatEnemyDefinition>(assetPath);
            if (!definition)
            {
                continue;
            }

            if (definition.maxHealth <= 0)
            {
                report.Error($"BoatEnemyDefinition has maxHealth <= 0: {assetPath}", definition);
            }

            if (definition.goldReward < 0)
            {
                report.Error($"BoatEnemyDefinition has goldReward < 0: {assetPath}", definition);
            }

            if (definition.damageToBase <= 0)
            {
                report.Warning($"BoatEnemyDefinition has damageToBase <= 0: {assetPath}", definition);
            }

            if (definition.prefabOverride && !definition.prefabOverride.GetComponent<BoatEnemy>())
            {
                report.Error($"BoatEnemyDefinition prefabOverride is not a BoatEnemy prefab: {assetPath}", definition);
            }
        }
    }

    private static void ValidatePath(string scenePath, global::Path path, ValidationReport report)
    {
        if (!path)
        {
            return;
        }

        path.RebuildFromChildren();
        if (path.Count < 2)
        {
            report.Error($"Path must contain at least 2 waypoints: {scenePath}", path);
            return;
        }

        Vector3 firstPosition = path.GetWaypoint(0) ? path.GetWaypoint(0).position : Vector3.zero;
        bool allStacked = true;
        for (int i = 1; i < path.Count; i++)
        {
            Transform waypoint = path.GetWaypoint(i);
            if (waypoint && Vector3.Distance(firstPosition, waypoint.position) > 0.01f)
            {
                allStacked = false;
                break;
            }
        }

        if (allStacked)
        {
            report.Error($"All waypoints appear stacked at the same position: {scenePath}", path);
        }
    }

    private static void ValidateSpawner(string scenePath, Spawner spawner, ValidationReport report)
    {
        if (!spawner)
        {
            return;
        }

        if (!spawner.boatPrefab)
        {
            report.Error($"Spawner is missing its default boat prefab: {scenePath}", spawner);
        }

        if ((spawner.waveDefinitions == null || spawner.waveDefinitions.Length == 0) && (spawner.TotalWaves <= 0))
        {
            report.Warning($"Spawner has no wave definitions assigned directly. Verify Main or LevelDefinition supplies them: {scenePath}", spawner);
        }
    }

    private static void ValidateMain(string scenePath, Main main, ValidationReport report)
    {
        if (!main)
        {
            return;
        }

        if (!main.spawner)
        {
            report.Warning($"Main has no direct Spawner reference. It will fall back to scene lookup: {scenePath}", main);
        }

        if (!main.path)
        {
            report.Warning($"Main has no direct Path reference. It will fall back to scene lookup: {scenePath}", main);
        }
    }

    private static void ValidateBuildNodes(string scenePath, BuildManager buildManager, global::Path path, ValidationReport report)
    {
        if (!buildManager || !path)
        {
            return;
        }

        BuildNode[] buildNodes = Object.FindObjectsByType<BuildNode>(FindObjectsInactive.Include);
        foreach (BuildNode buildNode in buildNodes)
        {
            if (!buildNode || buildNode.gameObject.scene != path.gameObject.scene)
            {
                continue;
            }

            if (IsPointOnWaterRoute(buildNode.transform.position, path, buildManager.waterRouteWidth * 0.5f))
            {
                report.Warning($"BuildNode overlaps the water route: {scenePath} -> {buildNode.name}", buildNode);
            }
        }
    }

    private static bool IsPointOnWaterRoute(Vector3 position, global::Path path, float halfWidth)
    {
        for (int i = 0; i < path.Count - 1; i++)
        {
            Transform a = path.GetWaypoint(i);
            Transform b = path.GetWaypoint(i + 1);
            if (!a || !b)
            {
                continue;
            }

            if (DistancePointToSegment(position, a.position, b.position) <= halfWidth)
            {
                return true;
            }
        }

        return false;
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 segment = segmentEnd - segmentStart;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= Mathf.Epsilon)
        {
            return Vector2.Distance(point, segmentStart);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / lengthSquared);
        return Vector2.Distance(point, segmentStart + segment * t);
    }

    private static T LoadPrefabComponent<T>(string assetPath, ValidationReport report) where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (!prefab)
        {
            report.Error($"Missing prefab asset: {assetPath}");
            return null;
        }

        return prefab.GetComponent<T>();
    }

    private static void ValidatePrefabAssetExists(string assetPath, ValidationReport report)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (!prefab)
        {
            report.Error($"Missing prefab asset: {assetPath}");
        }
    }

    private static void ValidatePrefabComponent<T>(string assetPath, ValidationReport report) where T : Component
    {
        T component = LoadPrefabComponent<T>(assetPath, report);
        if (!component)
        {
            report.Error($"Prefab is missing {typeof(T).Name}: {assetPath}");
        }
    }

    private static void ValidateHud(string scenePath, HUDController hud, ValidationReport report)
    {
        if (!hud)
        {
            return;
        }

        if (!hud.livesText)
        {
            report.Warning($"HUD is missing lives text reference: {scenePath}", hud);
        }

        if (!hud.goldText)
        {
            report.Warning($"HUD is missing gold text reference: {scenePath}", hud);
        }

        if (!hud.waveText)
        {
            report.Warning($"HUD is missing wave text reference: {scenePath}", hud);
        }

        SerializedObject hudObject = new SerializedObject(hud);
        if (hudObject.FindProperty("livesIconSprite")?.objectReferenceValue == null)
        {
            report.Warning($"HUD is missing lives icon sprite: {scenePath}", hud);
        }

        if (hudObject.FindProperty("waveIconSprite")?.objectReferenceValue == null)
        {
            report.Warning($"HUD is missing wave icon sprite: {scenePath}", hud);
        }

        if (hudObject.FindProperty("goldIconSprite")?.objectReferenceValue == null)
        {
            report.Warning($"HUD is missing gold icon sprite: {scenePath}", hud);
        }

        if (hudObject.FindProperty("pauseIconSprite")?.objectReferenceValue == null)
        {
            report.Warning($"HUD is missing pause icon sprite: {scenePath}", hud);
        }
    }

    private static void ValidateTowerShop(string scenePath, TowerShopUI shop, ValidationReport report)
    {
        if (!shop)
        {
            return;
        }

        if (!shop.ballistaPrefab)
        {
            report.Warning($"Tower shop is missing Ballista prefab: {scenePath}", shop);
        }

        if (!shop.cannonPrefab)
        {
            report.Warning($"Tower shop is missing Cannon prefab: {scenePath}", shop);
        }

        if (!shop.magicPrefab)
        {
            report.Warning($"Tower shop is missing Magic prefab: {scenePath}", shop);
        }

        if (!shop.airPrefab)
        {
            report.Warning($"Tower shop is missing Air prefab: {scenePath}", shop);
        }
    }

    private static bool AudioManagerHasConfiguredMusic(AudioManager audioManager, AudioManager.MusicCue cue)
    {
        SerializedObject serializedObject = new SerializedObject(audioManager);
        SerializedProperty musicLibrary = serializedObject.FindProperty("musicLibrary");
        if (musicLibrary != null)
        {
            for (int i = 0; i < musicLibrary.arraySize; i++)
            {
                SerializedProperty entry = musicLibrary.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("cue")?.enumValueIndex == (int)cue
                    && entry.FindPropertyRelative("clip")?.objectReferenceValue != null)
                {
                    return true;
                }
            }
        }

        string pathProperty = cue switch
        {
            AudioManager.MusicCue.MainMenu => "mainMenuMusicResourcePath",
            AudioManager.MusicCue.GameplayCalm => "gameplayCalmMusicResourcePath",
            AudioManager.MusicCue.GameplayBattle => "gameplayBattleMusicResourcePath",
            AudioManager.MusicCue.GameplayBoss => "gameplayBossMusicResourcePath",
            AudioManager.MusicCue.Victory => "victoryMusicResourcePath",
            AudioManager.MusicCue.Defeat => "defeatMusicResourcePath",
            _ => string.Empty,
        };

        if (string.IsNullOrWhiteSpace(pathProperty))
        {
            return false;
        }

        string configuredPath = serializedObject.FindProperty(pathProperty)?.stringValue;
        return !string.IsNullOrWhiteSpace(configuredPath);
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

    private sealed class ValidationReport
    {
        private int errors;
        private int warnings;

        public void Error(string message, Object context = null)
        {
            errors++;
            Debug.LogError($"[SetupValidator] {message}", context);
        }

        public void Warning(string message, Object context = null)
        {
            warnings++;
            Debug.LogWarning($"[SetupValidator] {message}", context);
        }

        public void LogSummary()
        {
            Debug.Log($"[SetupValidator] Validation complete. Errors: {errors}, Warnings: {warnings}");
        }
    }
}
#endif
