using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WaterTD;

public class SettingsController : MonoBehaviour
{
    [Header("Sliders (0..1)")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Labels (optional)")]
    public TextMeshProUGUI masterLabel;
    public TextMeshProUGUI musicLabel;
    public TextMeshProUGUI sfxLabel;

    [Header("Navigation")]
    public string backSceneName = SceneNames.MainMenu;

    private void Start()
    {
        // Load saved values or default to 0.8
        float master = PlayerPrefs.GetFloat("vol_master", 0.8f);
        float music  = PlayerPrefs.GetFloat("vol_music",  0.8f);
        float sfx    = PlayerPrefs.GetFloat("vol_sfx",    0.8f);

        if (masterSlider) { masterSlider.value = master; OnMasterChanged(master); }
        if (musicSlider)  { musicSlider.value  = music;  OnMusicChanged(music); }
        if (sfxSlider)    { sfxSlider.value    = sfx;    OnSFXChanged(sfx); }

        // Hook UI events
        if (masterSlider) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (musicSlider)  musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider)    sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    public void OnMasterChanged(float v)
    {
        AudioManager.Instance?.SetMasterVolume(v);
        PlayerPrefs.SetFloat("vol_master", v);
        if (masterLabel) masterLabel.text = $"Master: {(int)(v*100f)}%";
    }

    public void OnMusicChanged(float v)
    {
        AudioManager.Instance?.SetMusicVolume(v);
        PlayerPrefs.SetFloat("vol_music", v);
        if (musicLabel) musicLabel.text = $"Music: {(int)(v*100f)}%";
    }

    public void OnSFXChanged(float v)
    {
        AudioManager.Instance?.SetSFXVolume(v);
        PlayerPrefs.SetFloat("vol_sfx", v);
        if (sfxLabel) sfxLabel.text = $"SFX: {(int)(v*100f)}%";
    }

    public void OnClickBack()
    {
        // Optional: play a UI click
        AudioManager.Instance?.PlaySFX(SFX.Click);
        UnityEngine.SceneManagement.SceneManager.LoadScene(backSceneName);
    }
}

