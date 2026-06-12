using UnityEngine;

public class Main : MonoBehaviour
{
    [Header("Core Bootstrap")]
    [SerializeField] private GameManager gameManagerPrefab;
    [SerializeField] private AudioManager audioManagerPrefab;
    [SerializeField] private bool createFallbackManagersIfMissing = true;

    [Header("Scene References")]
    public GameManager gameManager;
    public Spawner spawner;
    public Path path;
    public BuildManager buildManager;

    [Header("Legacy Waves")]
    public Spawner.Wave[] waves;

    [Header("Data-Driven Waves")]
    public WaveDefinition[] waveDefinitions;

    [Header("Level Template")]
    public LevelDefinition levelDefinition;
    public bool autoStartOnSceneLoad = true;

    private void OnEnable()
    {
        if (spawner)
        {
            spawner.OnWaveStarted += HandleWaveStarted;
            spawner.OnIntermissionStarted += HandleIntermissionStarted;
            spawner.OnBossSpawned += HandleBossSpawned;
        }
    }

    private void OnDisable()
    {
        if (spawner)
        {
            spawner.OnWaveStarted -= HandleWaveStarted;
            spawner.OnIntermissionStarted -= HandleIntermissionStarted;
            spawner.OnBossSpawned -= HandleBossSpawned;
        }
    }

    private void Awake()
    {
        EnsureCoreManagers();
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (!spawner)     spawner     = FindAnyObjectByType<Spawner>();
        if (!path)        path        = FindAnyObjectByType<Path>();
        if (!buildManager) buildManager = FindAnyObjectByType<BuildManager>();
    }

    private void Start()
    {
        if (!spawner)
        {
            Debug.LogError("[Main] No Spawner found in the scene.", this);
            enabled = false;
            return;
        }

        if (!path)
        {
            Debug.LogError("[Main] No Path found in the scene.", this);
            enabled = false;
            return;
        }

        // Hand off scene references:
        spawner.path = path;
        ApplyLevelDefinition();
        AudioManager.Instance?.PlayMusicCue(AudioManager.MusicCue.GameplayCalm);

        if (autoStartOnSceneLoad)
        {
            BeginConfiguredRun();
        }
    }

    public void BeginConfiguredRun()
    {
        if (spawner == null)
        {
            return;
        }

        if (waveDefinitions != null && waveDefinitions.Length > 0)
        {
            spawner.Begin(waveDefinitions);
        }
        else
        {
            spawner.Begin(waves);
        }
    }

    private void ApplyLevelDefinition()
    {
        if (!levelDefinition)
        {
            return;
        }

        if (buildManager)
        {
            buildManager.useManualPlacementBounds = levelDefinition.useManualPlacementBounds;
            buildManager.placementBoundsCenter = levelDefinition.placementBoundsCenter;
            buildManager.placementBoundsSize = levelDefinition.placementBoundsSize;
            buildManager.waterRouteWidth = levelDefinition.waterRouteWidth;
            buildManager.CachePlacementBounds();
        }

        PathRouteVisualizer routeVisualizer = path ? path.GetComponent<PathRouteVisualizer>() : null;
        if (routeVisualizer)
        {
            routeVisualizer.SetRouteWidth(levelDefinition.waterRouteWidth);
        }

        if (levelDefinition.waveDefinitions != null && levelDefinition.waveDefinitions.Length > 0)
        {
            waveDefinitions = levelDefinition.waveDefinitions;
        }
    }

    private void EnsureCoreManagers()
    {
        if (GameManager.Instance == null)
        {
            if (gameManagerPrefab)
            {
                Instantiate(gameManagerPrefab);
            }
            else if (createFallbackManagersIfMissing)
            {
                new GameObject("GameManager (Runtime Fallback)", typeof(GameManager));
                Debug.LogWarning("[Main] No GameManager was present, so a runtime fallback GameManager was created.", this);
            }
        }

        if (AudioManager.Instance == null)
        {
            if (audioManagerPrefab)
            {
                Instantiate(audioManagerPrefab);
            }
            else if (createFallbackManagersIfMissing)
            {
                new GameObject("AudioManager (Runtime Fallback)", typeof(AudioManager));
                Debug.LogWarning("[Main] No AudioManager was present, so a runtime fallback AudioManager was created.", this);
            }
        }

        gameManager = GameManager.Instance;
    }

    private void HandleWaveStarted(int currentWave, int totalWaves)
    {
        AudioManager.Instance?.PlayMusicCue(AudioManager.MusicCue.GameplayBattle);
    }

    private void HandleIntermissionStarted(float duration)
    {
        AudioManager.Instance?.PlayMusicCue(AudioManager.MusicCue.GameplayCalm);
    }

    private void HandleBossSpawned(int waveIndex)
    {
        AudioManager.Instance?.PlayMusicCue(AudioManager.MusicCue.GameplayBoss);
    }
}
