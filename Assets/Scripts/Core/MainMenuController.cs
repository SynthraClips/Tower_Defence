using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public string fallbackGameplayScene = "Game";

    private void Awake()
    {
        // Ensure UI/input runs at normal speed after returning from gameplay
        Time.timeScale = 1f;
    }

    public void OnClickPlay()        => SceneManager.LoadScene(fallbackGameplayScene);
    public void OnClickLevelSelect() => SceneManager.LoadScene("LevelSelect");
    public void OnClickSettings()    => SceneManager.LoadScene("Settings");
	public void OnClickMenu()   	 => SceneManager.LoadScene("MainMenu");
	public void OnClickEasyL()   	 => SceneManager.LoadScene("Easy Level");
	public void OnClickMediumL()   	 => SceneManager.LoadScene("MediumLevel");
	public void OnClickHardL()   	 => SceneManager.LoadScene("HardLevel");
    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}