#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public static class BuildProfileSceneSync
{
    private static readonly string[] GameplaySceneOrder =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/LevelSelect.unity",
        "Assets/Scenes/Settings.unity",
        "Assets/Scenes/Easy Level.unity",
        "Assets/Scenes/MediumLevel.unity",
        "Assets/Scenes/HardLevel.unity",
    };

    [MenuItem("Tools/Tower Defence/Sync Build Scenes")]
    public static void SyncBuildScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>();

        foreach (string scenePath in GameplaySceneOrder)
        {
            if (File.Exists(scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[BuildProfileSceneSync] Missing scene: {scenePath}");
            }
        }

        string[] allSceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        foreach (string guid in allSceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            if (System.Array.IndexOf(GameplaySceneOrder, scenePath) >= 0)
            {
                continue;
            }

            // Keep test/prototype scenes visible in Build Settings but disabled.
            scenes.Add(new EditorBuildSettingsScene(scenePath, false));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        UnityEngine.Debug.Log($"[BuildProfileSceneSync] Synced {scenes.Count} scene(s) to Build Settings.");
    }
}
#endif
