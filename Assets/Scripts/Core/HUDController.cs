// HUDController.cs
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button levelSelectButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Icons")]
    [SerializeField] private Image livesIconImage;
    [SerializeField] private Image waveIconImage;
    [SerializeField] private Image goldIconImage;
    [SerializeField] private Sprite livesIconSprite;
    [SerializeField] private Sprite waveIconSprite;
    [SerializeField] private Sprite goldIconSprite;
    [SerializeField] private Sprite pauseIconSprite;

    [Header("Feedback")]
    [SerializeField] private Color spentColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color gainedColor = new Color(1f, 0.86f, 0.35f, 1f);
    [SerializeField] private Color lostColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Refs")]
    public GameManager gm;
    public Spawner spawner;
    private GameManager subscribedGameManager;
    private Spawner subscribedSpawner;

    private void Awake()
    {
        ResolveSceneReferences();
        EnsureFallbackUi();
        BindButtons();
    }

    private void OnEnable()
    {
        ResolveSceneReferences();
        RefreshSubscriptions();
    }

    private void Start()
    {
        ResolveSceneReferences();
        RefreshSubscriptions();
        BindButtons();

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
        SetPanelActive(pausePanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(victoryPanel, false);
        SetPanelActive(defeatPanel, false);
    }

    private void OnDisable()
    {
        if (subscribedGameManager != null)
        {
            subscribedGameManager.OnLivesChanged -= UpdateLives;
            subscribedGameManager.OnGoldChanged -= UpdateGold;
            subscribedGameManager.OnGameOver -= ShowGameOver;
            subscribedGameManager.OnVictory -= ShowVictory;
            subscribedGameManager.OnStateChanged -= UpdateState;
            subscribedGameManager.OnWaveChanged -= UpdateWave;
            subscribedGameManager.OnGoldSpent -= ShowGoldSpent;
            subscribedGameManager.OnGoldGained -= ShowGoldGained;
            subscribedGameManager.OnLivesLost -= ShowLivesLost;
            subscribedGameManager = null;
        }

        if (subscribedSpawner != null)
        {
            subscribedSpawner.OnWaveStarted -= UpdateWave;
            subscribedSpawner.OnIntermissionStarted -= ShowIntermission;
            subscribedSpawner.OnIntermissionTick -= TickIntermission;
            subscribedSpawner.OnIntermissionEnded -= HideIntermission;
            subscribedSpawner = null;
        }
    }

    private void ShowGameOver()
    {
        SetPanelActive(defeatPanel, true);
        SetPanelActive(victoryPanel, false);
        SetPanelActive(pausePanel, false);
        SetPanelActive(settingsPanel, false);
        if (gameOverText)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "Defeat";
        }
    }

    private void ShowVictory()
    {
        SetPanelActive(victoryPanel, true);
        SetPanelActive(defeatPanel, false);
        SetPanelActive(pausePanel, false);
        SetPanelActive(settingsPanel, false);
        if (victoryText)
        {
            victoryText.gameObject.SetActive(true);
            victoryText.text = "Victory";
        }
    }

    private void UpdateState(GameState state)
    {
        SetPanelActive(pausePanel, state == GameState.Paused);
        if (state != GameState.Victory)
        {
            SetPanelActive(victoryPanel, false);
        }

        if (state != GameState.Defeat)
        {
            SetPanelActive(defeatPanel, false);
        }

        if (state != GameState.Paused)
        {
            SetPanelActive(settingsPanel, false);
        }

        if (!stateText) return;
        stateText.text = state switch
        {
            GameState.Running => "Battle in progress",
            GameState.Paused => "Paused",
            GameState.Victory => "Victory",
            GameState.Defeat => "Defeat",
            GameState.Booting => "Preparing level",
            _ => state.ToString(),
        };
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

    private void ShowGoldSpent(int amount)
    {
        FloatingPopupSystem.Instance.ShowUiPopup(goldText, $"-{amount}", spentColor);
    }

    private void ShowGoldGained(int amount)
    {
        FloatingPopupSystem.Instance.ShowUiPopup(goldText, $"+{amount}", gainedColor);
    }

    private void ShowLivesLost(int amount)
    {
        FloatingPopupSystem.Instance.ShowUiPopup(livesText, $"-{amount}", lostColor);
    }

    public void OnPauseButtonPressed() => gm?.PauseGame();
    public void OnSettingsButtonPressed()
    {
        if (gm != null && gm.State == GameState.Paused)
        {
            SetPanelActive(pausePanel, false);
            SetPanelActive(settingsPanel, true);
        }
    }
    public void OnCloseSettingsButtonPressed()
    {
        if (gm != null && gm.State == GameState.Paused)
        {
            SetPanelActive(settingsPanel, false);
            SetPanelActive(pausePanel, true);
        }
    }
    public void OnResumeButtonPressed() => gm?.ResumeGame();
    public void OnRestartButtonPressed() => gm?.RestartCurrentScene();
    public void OnLevelSelectButtonPressed() => gm?.ReturnToLevelSelect();
    public void OnNextLevelButtonPressed()
    {
        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        int nextBuildIndex = activeScene.buildIndex + 1;
        if (nextBuildIndex >= 0 && nextBuildIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextBuildIndex);
            return;
        }

        gm?.ReturnToLevelSelect();
    }
    public void OnMainMenuButtonPressed() => gm?.ReturnToMainMenu();
    public void OnQuitButtonPressed()
    {
#if UNITY_EDITOR
        Debug.Log("[HUDController] Quit requested during editor play mode.");
#else
        Application.Quit();
#endif
    }

    private void BindButtons()
    {
        BindButton(pauseButton, OnPauseButtonPressed);
        BindButton(settingsButton, OnSettingsButtonPressed);
        BindButton(closeSettingsButton, OnCloseSettingsButtonPressed);
        BindButton(resumeButton, OnResumeButtonPressed);
        BindButton(restartButton, OnRestartButtonPressed);
        BindButton(levelSelectButton, OnLevelSelectButtonPressed);
        BindButton(nextLevelButton, OnNextLevelButtonPressed);
        BindButton(mainMenuButton, OnMainMenuButtonPressed);
        BindButton(quitButton, OnQuitButtonPressed);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (!button)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel)
        {
            panel.SetActive(active);
        }
    }

    private void EnsureFallbackUi()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            return;
        }

        TMP_FontAsset font = ResolveFont();
        EnsureHudIcons(canvas.transform as RectTransform);
        if (!pauseButton)
        {
            pauseButton = CreateStandaloneButton(
                canvas.transform as RectTransform,
                "PauseButton",
                "Pause",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-90f, -40f),
                font,
                OnPauseButtonPressed);
            AssignButtonIcon(pauseButton, ResolvePauseIcon());
        }

        if (!pausePanel)
        {
            pausePanel = CreateOverlayPanel(canvas.transform as RectTransform, "PausePanel", "Paused", font);
            CreateActionButton(pausePanel.transform as RectTransform, "ResumeButton", "Resume", new Vector2(0f, 20f), font, OnResumeButtonPressed);
            CreateActionButton(pausePanel.transform as RectTransform, "SettingsButton", "Settings", new Vector2(0f, -30f), font, OnSettingsButtonPressed);
            CreateActionButton(pausePanel.transform as RectTransform, "RestartButton", "Restart", new Vector2(0f, -80f), font, OnRestartButtonPressed);
            CreateActionButton(pausePanel.transform as RectTransform, "LevelSelectButton", "Level Select", new Vector2(0f, -130f), font, OnLevelSelectButtonPressed);
            CreateActionButton(pausePanel.transform as RectTransform, "MainMenuButton", "Main Menu", new Vector2(0f, -180f), font, OnMainMenuButtonPressed);
            CreateActionButton(pausePanel.transform as RectTransform, "QuitButton", "Quit Game", new Vector2(0f, -230f), font, OnQuitButtonPressed);
        }

        if (!settingsPanel)
        {
            settingsPanel = CreateOverlayPanel(canvas.transform as RectTransform, "SettingsPanel", "Audio Settings", font);
            CreateSliderGroup(settingsPanel.transform as RectTransform, "Master", new Vector2(0f, 45f), PlayerPrefs.GetFloat("vol_master", 0.8f), v =>
            {
                AudioManager.Instance?.SetMasterVolume(v);
                PlayerPrefs.SetFloat("vol_master", v);
            }, font);
            CreateSliderGroup(settingsPanel.transform as RectTransform, "Music", new Vector2(0f, 0f), PlayerPrefs.GetFloat("vol_music", 0.8f), v =>
            {
                AudioManager.Instance?.SetMusicVolume(v);
                PlayerPrefs.SetFloat("vol_music", v);
            }, font);
            CreateSliderGroup(settingsPanel.transform as RectTransform, "SFX", new Vector2(0f, -45f), PlayerPrefs.GetFloat("vol_sfx", 0.8f), v =>
            {
                AudioManager.Instance?.SetSFXVolume(v);
                PlayerPrefs.SetFloat("vol_sfx", v);
            }, font);
            CreateActionButton(settingsPanel.transform as RectTransform, "CloseSettingsButton", "Back", new Vector2(0f, -120f), font, OnCloseSettingsButtonPressed);
        }

        if (!resumeButton)
        {
            resumeButton = FindButtonInChildren(pausePanel, "ResumeButton");
        }

        if (!victoryPanel)
        {
            victoryPanel = CreateOverlayPanel(canvas.transform as RectTransform, "VictoryPanel", "Victory", font);
            CreateActionButton(victoryPanel.transform as RectTransform, "NextLevelButton", "Next Level", new Vector2(0f, 35f), font, OnNextLevelButtonPressed);
            CreateActionButton(victoryPanel.transform as RectTransform, "RestartButton", "Restart Level", new Vector2(0f, -15f), font, OnRestartButtonPressed);
            CreateActionButton(victoryPanel.transform as RectTransform, "LevelSelectButton", "Level Select", new Vector2(0f, -65f), font, OnLevelSelectButtonPressed);
            CreateActionButton(victoryPanel.transform as RectTransform, "MainMenuButton", "Main Menu", new Vector2(0f, -115f), font, OnMainMenuButtonPressed);
        }

        if (!defeatPanel)
        {
            defeatPanel = CreateOverlayPanel(canvas.transform as RectTransform, "DefeatPanel", "Defeat", font);
            CreateActionButton(defeatPanel.transform as RectTransform, "RestartButton", "Restart Level", new Vector2(0f, 10f), font, OnRestartButtonPressed);
            CreateActionButton(defeatPanel.transform as RectTransform, "LevelSelectButton", "Level Select", new Vector2(0f, -40f), font, OnLevelSelectButtonPressed);
            CreateActionButton(defeatPanel.transform as RectTransform, "MainMenuButton", "Main Menu", new Vector2(0f, -90f), font, OnMainMenuButtonPressed);
        }

        if (!settingsButton)
        {
            settingsButton = FindButtonInChildren(pausePanel, "SettingsButton");
        }

        if (!quitButton)
        {
            quitButton = FindButtonInChildren(pausePanel, "QuitButton");
        }

        if (!closeSettingsButton)
        {
            closeSettingsButton = FindButtonInChildren(settingsPanel, "CloseSettingsButton");
        }

        if (!mainMenuButton)
        {
            mainMenuButton = FindButtonInChildren(pausePanel, "MainMenuButton") ?? FindButtonInChildren(victoryPanel, "MainMenuButton") ?? FindButtonInChildren(defeatPanel, "MainMenuButton");
        }

        if (!restartButton)
        {
            restartButton = FindButtonInChildren(pausePanel, "RestartButton") ?? FindButtonInChildren(victoryPanel, "RestartButton") ?? FindButtonInChildren(defeatPanel, "RestartButton");
        }

        if (!levelSelectButton)
        {
            levelSelectButton = FindButtonInChildren(pausePanel, "LevelSelectButton") ?? FindButtonInChildren(victoryPanel, "LevelSelectButton") ?? FindButtonInChildren(defeatPanel, "LevelSelectButton");
        }

        if (!nextLevelButton)
        {
            nextLevelButton = FindButtonInChildren(victoryPanel, "NextLevelButton");
        }
    }

    private TMP_FontAsset ResolveFont()
    {
        if (livesText && livesText.font)
        {
            return livesText.font;
        }

        if (goldText && goldText.font)
        {
            return goldText.font;
        }

        if (waveText && waveText.font)
        {
            return waveText.font;
        }

        TextMeshProUGUI anyText = GetComponentInChildren<TextMeshProUGUI>(true);
        return anyText ? anyText.font : null;
    }

    private void ResolveSceneReferences()
    {
        gm = GameManager.Instance ? GameManager.Instance : FindAnyObjectByType<GameManager>();
        spawner = FindAnyObjectByType<Spawner>();

        livesText = FindNamedComponent<TextMeshProUGUI>("LivesText") ?? livesText;
        goldText = FindNamedComponent<TextMeshProUGUI>("GoldText") ?? goldText;
        waveText = FindNamedComponent<TextMeshProUGUI>("WaveText") ?? waveText;
        waveTimerText = FindNamedComponent<TextMeshProUGUI>("WaveTimerText") ?? waveTimerText;
        gameOverText = FindNamedComponent<TextMeshProUGUI>("gameOverText") ?? gameOverText;
        victoryText = FindNamedComponent<TextMeshProUGUI>("victoryText") ?? victoryText;
        stateText = FindNamedComponent<TextMeshProUGUI>("stateText") ?? stateText;

        pausePanel = ResolveChildGameObject(null, "PausePanel") ?? pausePanel;
        settingsPanel = ResolveChildGameObject(null, "SettingsPanel") ?? settingsPanel;
        victoryPanel = ResolveChildGameObject(null, "VictoryPanel") ?? victoryPanel;
        defeatPanel = ResolveChildGameObject(null, "DefeatPanel") ?? defeatPanel;

        pauseButton = FindNamedComponent<Button>("PauseButton") ?? pauseButton;
        settingsButton = FindNamedComponent<Button>("SettingsButton") ?? settingsButton;
        closeSettingsButton = FindNamedComponent<Button>("CloseSettingsButton") ?? closeSettingsButton;
        resumeButton = FindNamedComponent<Button>("ResumeButton") ?? resumeButton;
        restartButton = FindNamedComponent<Button>("RestartButton") ?? restartButton;
        levelSelectButton = FindNamedComponent<Button>("LevelSelectButton") ?? levelSelectButton;
        nextLevelButton = FindNamedComponent<Button>("NextLevelButton") ?? nextLevelButton;
        mainMenuButton = FindNamedComponent<Button>("MainMenuButton") ?? mainMenuButton;
        quitButton = FindNamedComponent<Button>("QuitButton") ?? quitButton;

        livesIconImage = FindNamedComponent<Image>("LivesIcon") ?? livesIconImage;
        waveIconImage = FindNamedComponent<Image>("WaveIcon") ?? waveIconImage;
        goldIconImage = FindNamedComponent<Image>("GoldIcon") ?? goldIconImage;
    }

    private void RefreshSubscriptions()
    {
        if (subscribedGameManager != gm)
        {
            if (subscribedGameManager != null)
            {
                subscribedGameManager.OnLivesChanged -= UpdateLives;
                subscribedGameManager.OnGoldChanged -= UpdateGold;
                subscribedGameManager.OnGameOver -= ShowGameOver;
                subscribedGameManager.OnVictory -= ShowVictory;
                subscribedGameManager.OnStateChanged -= UpdateState;
                subscribedGameManager.OnWaveChanged -= UpdateWave;
                subscribedGameManager.OnGoldSpent -= ShowGoldSpent;
                subscribedGameManager.OnGoldGained -= ShowGoldGained;
                subscribedGameManager.OnLivesLost -= ShowLivesLost;
            }

            subscribedGameManager = gm;

            if (subscribedGameManager != null)
            {
                subscribedGameManager.OnLivesChanged += UpdateLives;
                subscribedGameManager.OnGoldChanged += UpdateGold;
                subscribedGameManager.OnGameOver += ShowGameOver;
                subscribedGameManager.OnVictory += ShowVictory;
                subscribedGameManager.OnStateChanged += UpdateState;
                subscribedGameManager.OnWaveChanged += UpdateWave;
                subscribedGameManager.OnGoldSpent += ShowGoldSpent;
                subscribedGameManager.OnGoldGained += ShowGoldGained;
                subscribedGameManager.OnLivesLost += ShowLivesLost;
            }
        }

        if (subscribedSpawner != spawner)
        {
            if (subscribedSpawner != null)
            {
                subscribedSpawner.OnWaveStarted -= UpdateWave;
                subscribedSpawner.OnIntermissionStarted -= ShowIntermission;
                subscribedSpawner.OnIntermissionTick -= TickIntermission;
                subscribedSpawner.OnIntermissionEnded -= HideIntermission;
            }

            subscribedSpawner = spawner;

            if (subscribedSpawner != null)
            {
                subscribedSpawner.OnWaveStarted += UpdateWave;
                subscribedSpawner.OnIntermissionStarted += ShowIntermission;
                subscribedSpawner.OnIntermissionTick += TickIntermission;
                subscribedSpawner.OnIntermissionEnded += HideIntermission;
            }
        }
    }

    private GameObject ResolveChildGameObject(GameObject current, string objectName)
    {
        if (current && current.scene.IsValid())
        {
            return current;
        }

        Transform found = FindNamedTransform(objectName);
        return found ? found.gameObject : null;
    }

    private T FindNamedComponent<T>(string objectName) where T : Component
    {
        Transform found = FindNamedTransform(objectName);
        return found ? found.GetComponent<T>() : null;
    }

    private Transform FindNamedTransform(string objectName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child && child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }

    private GameObject CreateOverlayPanel(RectTransform parent, string panelName, string title, TMP_FontAsset font)
    {
        if (!parent)
        {
            return null;
        }

        GameObject panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(360f, 430f);
        rect.anchoredPosition = Vector2.zero;

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.1f, 0.16f, 0.92f);

        CreateLabel(rect, $"{panelName}_Title", title, new Vector2(0f, 85f), 34f, font);
        panelObject.SetActive(false);
        return panelObject;
    }

    private Button CreateStandaloneButton(
        RectTransform parent,
        string buttonName,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        TMP_FontAsset font,
        UnityEngine.Events.UnityAction onClick)
    {
        if (!parent)
        {
            return null;
        }

        GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(120f, 36f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.14f, 0.35f, 0.52f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        if (!string.IsNullOrWhiteSpace(label))
        {
            CreateLabel(rect, $"{buttonName}_Label", label, Vector2.zero, 24f, font);
        }
        return button;
    }

    private Button CreateActionButton(
        RectTransform parent,
        string buttonName,
        string label,
        Vector2 anchoredPosition,
        TMP_FontAsset font,
        UnityEngine.Events.UnityAction onClick)
    {
        return CreateStandaloneButton(
            parent,
            buttonName,
            label,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            anchoredPosition,
            font,
            onClick);
    }

    private void CreateLabel(RectTransform parent, string objectName, string text, Vector2 anchoredPosition, float fontSize, TMP_FontAsset font)
    {
        if (!parent)
        {
            return;
        }

        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(240f, 36f);
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI tmp = labelObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (font)
        {
            tmp.font = font;
        }
    }

    private void EnsureHudIcons(RectTransform canvasRect)
    {
        livesIconSprite = livesIconSprite ? livesIconSprite : Resources.Load<Sprite>("UI/ui_icon_health");
        waveIconSprite = waveIconSprite ? waveIconSprite : Resources.Load<Sprite>("UI/ui_icon_wave");
        goldIconSprite = goldIconSprite ? goldIconSprite : Resources.Load<Sprite>("UI/ui_icon_coins");
        pauseIconSprite = pauseIconSprite ? pauseIconSprite : Resources.Load<Sprite>("UI/ui_icon_pause");

        livesIconImage = EnsureIconImage(livesText, livesIconImage, "LivesIcon", livesIconSprite);
        waveIconImage = EnsureIconImage(waveText, waveIconImage, "WaveIcon", waveIconSprite);
        goldIconImage = EnsureIconImage(goldText, goldIconImage, "GoldIcon", goldIconSprite);
    }

    private static Image EnsureIconImage(TextMeshProUGUI label, Image existingImage, string iconName, Sprite sprite)
    {
        if (!label || !sprite)
        {
            return existingImage;
        }

        Image iconImage = existingImage;
        if (!iconImage)
        {
            Transform found = label.transform.parent ? label.transform.parent.Find(iconName) : null;
            if (found)
            {
                iconImage = found.GetComponent<Image>();
            }
        }

        if (!iconImage)
        {
            GameObject iconObject = new GameObject(iconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(label.transform, false);
            iconImage = iconObject.GetComponent<Image>();
        }
        else if (iconImage.transform.parent != label.transform)
        {
            iconImage.transform.SetParent(label.transform, false);
        }

        RectTransform iconRect = iconImage.transform as RectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(28f, 28f);
        iconRect.localScale = Vector3.one;
        iconRect.anchoredPosition = new Vector2(-24f, 0f);

        iconImage.sprite = sprite;
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        return iconImage;
    }

    private Sprite ResolvePauseIcon()
    {
        return pauseIconSprite ? pauseIconSprite : Resources.Load<Sprite>("UI/ui_icon_pause");
    }

    private static void AssignButtonIcon(Button button, Sprite iconSprite)
    {
        if (!button || !iconSprite)
        {
            return;
        }

        Transform iconTransform = button.transform.Find("Icon");
        Image iconImage = iconTransform ? iconTransform.GetComponent<Image>() : null;
        if (!iconImage)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            iconImage = iconObject.GetComponent<Image>();
        }

        RectTransform iconRect = iconImage.transform as RectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(22f, 22f);
        iconRect.anchoredPosition = Vector2.zero;
        iconImage.sprite = iconSprite;
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
    }

    private void CreateSliderGroup(RectTransform parent, string label, Vector2 anchoredPosition, float value, UnityEngine.Events.UnityAction<float> onValueChanged, TMP_FontAsset font)
    {
        GameObject groupObject = new GameObject($"{label}SliderGroup", typeof(RectTransform));
        groupObject.transform.SetParent(parent, false);
        RectTransform groupRect = groupObject.GetComponent<RectTransform>();
        groupRect.anchorMin = new Vector2(0.5f, 0.5f);
        groupRect.anchorMax = new Vector2(0.5f, 0.5f);
        groupRect.pivot = new Vector2(0.5f, 0.5f);
        groupRect.sizeDelta = new Vector2(240f, 32f);
        groupRect.anchoredPosition = anchoredPosition;

        CreateLabel(groupRect, $"{label}Label", label, new Vector2(-95f, 0f), 20f, font);

        GameObject sliderObject = new GameObject($"{label}Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(groupRect, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(140f, 18f);
        sliderRect.anchoredPosition = new Vector2(40f, 0f);

        Image background = sliderObject.AddComponent<Image>();
        background.color = new Color(0.16f, 0.22f, 0.31f, 0.95f);

        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObject.transform.SetParent(sliderRect, false);
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(5f, 5f);
        fillAreaRect.offsetMax = new Vector2(-5f, -5f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(fillAreaRect, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color(0.36f, 0.72f, 0.98f, 1f);

        GameObject handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleSlideArea.transform.SetParent(sliderRect, false);
        RectTransform handleSlideRect = handleSlideArea.GetComponent<RectTransform>();
        handleSlideRect.anchorMin = Vector2.zero;
        handleSlideRect.anchorMax = Vector2.one;
        handleSlideRect.offsetMin = Vector2.zero;
        handleSlideRect.offsetMax = Vector2.zero;

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(handleSlideRect, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16f, 24f);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = Color.white;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(value);
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.onValueChanged.AddListener(onValueChanged);
    }

    private static Button FindButtonInChildren(GameObject parent, string buttonName)
    {
        if (!parent)
        {
            return null;
        }

        Transform buttonTransform = parent.transform.Find(buttonName);
        return buttonTransform ? buttonTransform.GetComponent<Button>() : null;
    }

}
