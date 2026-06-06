using UnityEngine;

public class Main : MonoBehaviour
{
    [Header("Scene References")]
    public GameManager gameManager;
    public Spawner spawner;
    public Path path;

    [Header("Legacy Waves")]
    public Spawner.Wave[] waves;

    [Header("Data-Driven Waves")]
    public WaveDefinition[] waveDefinitions;

    private void Awake()
    {
        if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        if (!spawner)     spawner     = FindAnyObjectByType<Spawner>();
        if (!path)        path        = FindAnyObjectByType<Path>();
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

        if (waveDefinitions != null && waveDefinitions.Length > 0)
        {
            spawner.Begin(waveDefinitions);
        }
        else
        {
            spawner.Begin(waves);
        }
    }
}
