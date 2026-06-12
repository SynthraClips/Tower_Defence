using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using WaterTD;

public enum GameState
{
    Booting,
    Running,
    Paused,
    Victory,
    Defeat,
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Stats")]
    public int startingLives = 20;
    public int startingGold = 100;
    public int lives = 20;
    public int gold = 100;

    [Header("Runtime")]
    [SerializeField] private GameState state = GameState.Booting;
    [SerializeField] private int currentWave;
    [SerializeField] private int totalWaves;

    public event Action<int> OnLivesChanged;
    public event Action<int> OnGoldChanged;
    public event Action<int> OnLivesLost;
    public event Action<int> OnGoldSpent;
    public event Action<int> OnGoldGained;
    public event Action<GameState> OnStateChanged;
    public event Action<int, int> OnWaveChanged;
    public event Action OnGameOver;
    public event Action OnVictory;

    public GameState State => state;
    public int CurrentWave => currentWave;
    public int TotalWaves => totalWaves;
    public bool IsGameOver => state == GameState.Defeat;
    public bool IsPaused => state == GameState.Paused;
    public bool HasActiveRun => state == GameState.Running || state == GameState.Paused || state == GameState.Victory || state == GameState.Defeat;

    private float defaultFixedDeltaTime;
    private float resumeTimeScale = 1f;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ChangeState(GameState.Booting);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    private void Start()
    {
        OnLivesChanged?.Invoke(lives);
        OnGoldChanged?.Invoke(gold);
        OnWaveChanged?.Invoke(currentWave, totalWaves);
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            TogglePause();
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartCurrentScene();
        }

        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            ReturnToMainMenu();
        }
    }

    public void StartRun(int waveCount)
    {
        totalWaves = Mathf.Max(0, waveCount);
        currentWave = 0;
        lives = Mathf.Max(1, startingLives);
        gold = Mathf.Max(0, startingGold);
        SetGameplayTimeScale(1f);
        resumeTimeScale = 1f;
        ChangeState(GameState.Running);
        OnLivesChanged?.Invoke(lives);
        OnGoldChanged?.Invoke(gold);
        OnWaveChanged?.Invoke(currentWave, totalWaves);
    }

    public void SetCurrentWave(int waveIndex)
    {
        currentWave = Mathf.Clamp(waveIndex, 0, Mathf.Max(0, totalWaves));
        OnWaveChanged?.Invoke(currentWave, totalWaves);
    }

    public void PauseGame()
    {
        if (state != GameState.Running) return;
        resumeTimeScale = Mathf.Max(0.01f, Time.timeScale);
        AudioManager.Instance?.PauseMusic();
        SetGameplayTimeScale(0f);
        ChangeState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (state != GameState.Paused) return;
        SetGameplayTimeScale(resumeTimeScale);
        AudioManager.Instance?.ResumeMusic();
        ChangeState(GameState.Running);
    }

    public void TogglePause()
    {
        if (state == GameState.Paused)
        {
            ResumeGame();
        }
        else if (state == GameState.Running)
        {
            PauseGame();
        }
    }

    public void RestartCurrentScene()
    {
        ResetGameplayTimeForSceneChange();
        ChangeState(GameState.Booting);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToLevelSelect()
    {
        ResetGameplayTimeForSceneChange();
        ChangeState(GameState.Booting);
        SceneManager.LoadScene(SceneNames.LevelSelect);
    }

    public void ReturnToMainMenu()
    {
        ResetGameplayTimeForSceneChange();
        ChangeState(GameState.Booting);
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    public void AddGold(int amount)
    {
        int appliedAmount = Mathf.Max(0, amount);
        if (appliedAmount <= 0)
        {
            return;
        }

        gold += appliedAmount;
        OnGoldChanged?.Invoke(gold);
        OnGoldGained?.Invoke(appliedAmount);
    }

    public bool SpendGold(int amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        OnGoldSpent?.Invoke(amount);
        return true;
    }

    public void HealLife(int amount, int maxCap = 999)
    {
        if (amount <= 0) return;
        lives += amount;
        lives = Mathf.Min(lives, maxCap);
        OnLivesChanged?.Invoke(lives);
    }

    public void TakeLifeDamage(int amount)
    {
        if (state == GameState.Defeat || state == GameState.Victory) return;

        int appliedDamage = Mathf.Max(1, amount);
        lives -= appliedDamage;
        if (lives < 0) lives = 0;
        OnLivesChanged?.Invoke(lives);
        OnLivesLost?.Invoke(appliedDamage);

        if (lives <= 0)
        {
            TriggerGameOver();
        }
    }

    public void TriggerVictory()
    {
        if (state == GameState.Victory || state == GameState.Defeat) return;

        Debug.Log("Victory!");
        resumeTimeScale = Mathf.Max(0.01f, Time.timeScale);
        SetGameplayTimeScale(0f);
        ChangeState(GameState.Victory);
        AudioManager.Instance?.PlayMusicCue(AudioManager.MusicCue.Victory);
        OnVictory?.Invoke();
    }

    public void TriggerGameOver()
    {
        if (state == GameState.Defeat) return;

        Debug.Log("Game Over!");
        resumeTimeScale = Mathf.Max(0.01f, Time.timeScale);
        SetGameplayTimeScale(0f);
        ChangeState(GameState.Defeat);
        AudioManager.Instance?.PlayMusicCue(AudioManager.MusicCue.Defeat);
        OnGameOver?.Invoke();
    }

    private void ChangeState(GameState newState)
    {
        state = newState;
        OnStateChanged?.Invoke(state);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetGameplayTimeForSceneChange();

        if (!IsGameplayScene(scene.name))
        {
            ChangeState(GameState.Booting);
        }
    }

    private bool IsGameplayScene(string sceneName)
    {
        return sceneName == SceneNames.EasyLevel
            || sceneName == SceneNames.MediumLevel
            || sceneName == SceneNames.HardLevel
            || sceneName == SceneNames.Game;
    }

    private void ResetGameplayTimeForSceneChange()
    {
        resumeTimeScale = 1f;
        SetGameplayTimeScale(1f);
    }

    private void SetGameplayTimeScale(float scale)
    {
        Time.timeScale = Mathf.Max(0f, scale);
        Time.fixedDeltaTime = defaultFixedDeltaTime * (Time.timeScale <= 0f ? 1f : Time.timeScale);
    }
}
