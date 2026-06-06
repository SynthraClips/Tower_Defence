using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer & Groups")]
    public AudioMixer masterMixer;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [System.Serializable]
    public struct SFXEntry
    {
        public SFX key;
        public AudioClip clip;
    }

    [Header("Sound Library")]
    public List<SFXEntry> sfxLibrary = new List<SFXEntry>();

    private Dictionary<SFX, AudioClip> sfxDict = new Dictionary<SFX, AudioClip>();
    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource   = gameObject.AddComponent<AudioSource>();

        musicSource.outputAudioMixerGroup = musicGroup;
        sfxSource.outputAudioMixerGroup   = sfxGroup;

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource.playOnAwake   = false;

        // Build lookup dictionary
        foreach (var entry in sfxLibrary)
            if (entry.clip != null)
                sfxDict[entry.key] = entry.clip;

        // Load saved volumes (same as before)
        float master = PlayerPrefs.GetFloat("vol_master", 0.8f);
        float music  = PlayerPrefs.GetFloat("vol_music",  0.8f);
        float sfx    = PlayerPrefs.GetFloat("vol_sfx",    0.8f);
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
    }

    // --- Volume controls ---
    public void SetMasterVolume(float linear) => SetDb("MasterVol", linear);
    public void SetMusicVolume (float linear) => SetDb("MusicVol",  linear);
    public void SetSFXVolume   (float linear) => SetDb("SFXVol",    linear);

    private void SetDb(string exposedParam, float linear)
    {
        float value = Mathf.Clamp01(linear);
        float dB = (value <= 0.0001f) ? -80f : Mathf.Log10(value) * 20f;
        masterMixer.SetFloat(exposedParam, dB);
    }

    // --- Playback ---
	public void TestPlay(SFX key) => PlaySFX(key);
    public void PlaySFX(SFX key)
    {
        if (sfxDict.TryGetValue(key, out var clip) && clip)
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] No clip assigned for {key}");
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (!clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }
    public void StopMusic() => musicSource.Stop();
}
