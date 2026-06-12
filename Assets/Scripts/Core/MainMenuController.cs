using UnityEngine;
using UnityEngine.SceneManagement;
using WaterTD;

public class MainMenuController : MonoBehaviour
{
    public string fallbackGameplayScene = SceneNames.EasyLevel;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        TryPlayMenuMusic();
    }

    public void OnClickPlay() => LoadSceneSafe(fallbackGameplayScene, SceneNames.EasyLevel);
    public void OnClickLevelSelect() => LoadSceneSafe(SceneNames.LevelSelect);
    public void OnClickSettings() => LoadSceneSafe(SceneNames.Settings);
    public void OnClickMenu() => LoadSceneSafe(SceneNames.MainMenu);
    public void OnClickEasyL() => LoadSceneSafe(SceneNames.EasyLevel);
    public void OnClickMediumL() => LoadSceneSafe(SceneNames.MediumLevel);
    public void OnClickHardL() => LoadSceneSafe(SceneNames.HardLevel);

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void LoadSceneSafe(string requestedScene, string fallbackScene = null)
    {
        string sceneToLoad = string.IsNullOrWhiteSpace(requestedScene) ? fallbackScene : requestedScene;
        if (string.IsNullOrWhiteSpace(sceneToLoad))
        {
            Debug.LogWarning("[MainMenuController] No scene name supplied.");
            return;
        }

        if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
            return;
        }

        if (!string.IsNullOrWhiteSpace(fallbackScene) && Application.CanStreamedLevelBeLoaded(fallbackScene))
        {
            Debug.LogWarning($"[MainMenuController] Scene '{sceneToLoad}' was not in the build profile. Loading fallback '{fallbackScene}'.");
            SceneManager.LoadScene(fallbackScene);
            return;
        }

        Debug.LogWarning($"[MainMenuController] Scene '{sceneToLoad}' is not in the build profile or is misspelled.");
    }

    private void TryPlayMenuMusic()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }
        AudioManager.Instance.PlayMusicCue(AudioManager.MusicCue.MainMenu);
    }
}
