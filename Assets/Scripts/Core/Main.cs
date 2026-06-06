using UnityEngine;

public class Main : MonoBehaviour
{
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

    private void Awake()
    {
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

        if (levelDefinition.waveDefinitions != null && levelDefinition.waveDefinitions.Length > 0)
        {
            waveDefinitions = levelDefinition.waveDefinitions;
        }
    }
}
