// HUDController.cs
using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI waveTimerText;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI victoryText;
    public TextMeshProUGUI stateText;

    [Header("Refs")]
    public GameManager gm;
    public Spawner spawner;

    private void Awake()
    {
        if (!gm) gm = GameManager.Instance;
        if (!spawner) spawner = FindAnyObjectByType<Spawner>();
    }

    private void OnEnable()
    {
        if (gm != null)
        {
            gm.OnLivesChanged += UpdateLives;
            gm.OnGoldChanged += UpdateGold;
            gm.OnGameOver += ShowGameOver;
            gm.OnVictory += ShowVictory;
            gm.OnStateChanged += UpdateState;
            gm.OnWaveChanged += UpdateWave;
        }

        if (spawner != null)
        {
            spawner.OnWaveStarted += UpdateWave;
            spawner.OnIntermissionStarted += ShowIntermission;
            spawner.OnIntermissionTick += TickIntermission;
            spawner.OnIntermissionEnded += HideIntermission;
        }
    }

    private void Start()
    {
        if (gm != null)
        {
            UpdateLives(gm.lives);
            UpdateGold(gm.gold);
            UpdateState(gm.State);
            UpdateWave(gm.CurrentWave, gm.TotalWaves);
        }

        if (waveTimerText) waveTimerText.gameObject.SetActive(false);
        if (gameOverText) gameOverText.gameObject.SetActive(false);
        if (victoryText) victoryText.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (gm != null)
        {
            gm.OnLivesChanged -= UpdateLives;
            gm.OnGoldChanged -= UpdateGold;
            gm.OnGameOver -= ShowGameOver;
            gm.OnVictory -= ShowVictory;
            gm.OnStateChanged -= UpdateState;
            gm.OnWaveChanged -= UpdateWave;
        }

        if (spawner != null)
        {
            spawner.OnWaveStarted -= UpdateWave;
            spawner.OnIntermissionStarted -= ShowIntermission;
            spawner.OnIntermissionTick -= TickIntermission;
            spawner.OnIntermissionEnded -= HideIntermission;
        }
    }

    private void ShowGameOver()
    {
        if (gameOverText)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "GAME OVER!";
        }
    }

    private void ShowVictory()
    {
        if (victoryText)
        {
            victoryText.gameObject.SetActive(true);
            victoryText.text = "VICTORY!";
        }
    }

    private void UpdateState(GameState state)
    {
        if (!stateText) return;
        stateText.text = $"State: {state}";
    }

    private void UpdateLives(int v)
    {
        if (livesText) livesText.text = $"Lives: {v}";
    }

    private void UpdateGold(int v)
    {
        if (goldText) goldText.text = $"Gold:  {v:N0}";
    }

    private void UpdateWave(int current, int total)
    {
        if (!waveText) return;
        waveText.text = total > 0 ? $"Wave:  {current} / {total}" : "Wave: -";
        HideIntermission(); // hide timer while a wave is spawning
    }

    private void ShowIntermission(float duration)
    {
        if (!waveTimerText) return;
        waveTimerText.gameObject.SetActive(true);
        waveTimerText.text = $"Next wave in {duration:0.0}s";
    }

    private void TickIntermission(float remaining)
    {
        if (!waveTimerText) return;
        waveTimerText.text = $"Next wave in {remaining:0.0}s";
    }

    private void HideIntermission()
    {
        if (waveTimerText) waveTimerText.gameObject.SetActive(false);
    }
}
