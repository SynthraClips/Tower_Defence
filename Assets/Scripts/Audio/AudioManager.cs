using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

[DefaultExecutionOrder(-1000)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum MusicCue
    {
        MainMenu,
        GameplayCalm,
        GameplayBattle,
        GameplayBoss,
        Victory,
        Defeat,
    }

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

    [System.Serializable]
    public struct MusicEntry
    {
        public MusicCue cue;
        public AudioClip clip;
        public bool loop;
    }

    [Header("Sound Library")]
    public List<SFXEntry> sfxLibrary = new List<SFXEntry>();
    [SerializeField] private List<MusicEntry> musicLibrary = new List<MusicEntry>();
    [SerializeField] private bool allowPlaceholderResourceFallbacks = true;
    [SerializeField] private string mainMenuMusicResourcePath = "Music/main_menu_music";
    [SerializeField] private string gameplayCalmMusicResourcePath = "Music/level_calm_open_water_loop";
    [SerializeField] private string gameplayBattleMusicResourcePath = "Music/level_battle_wave_loop";
    [SerializeField] private string gameplayBossMusicResourcePath = "Music/boss_wave_storm_loop";
    [SerializeField] private string victoryMusicResourcePath = string.Empty;
    [SerializeField] private string defeatMusicResourcePath = string.Empty;

    private Dictionary<SFX, AudioClip> sfxDict = new Dictionary<SFX, AudioClip>();
    private Dictionary<MusicCue, MusicEntry> musicDict = new Dictionary<MusicCue, MusicEntry>();
    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();

        if (musicSource)
        {
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource)
        {
            sfxSource.outputAudioMixerGroup = sfxGroup;
            sfxSource.playOnAwake = false;
        }

        // Build lookup dictionaries
        musicDict.Clear();
        foreach (var entry in musicLibrary)
        {
            if (entry.clip != null)
            {
                musicDict[entry.cue] = entry;
            }
        }

        sfxDict.Clear();
        foreach (var entry in sfxLibrary)
        {
            if (entry.clip != null)
            {
                sfxDict[entry.key] = entry.clip;
            }
        }

        // Load saved volumes (same as before)
        float master = PlayerPrefs.GetFloat("vol_master", 0.8f);
        float music  = PlayerPrefs.GetFloat("vol_music",  0.8f);
        float sfx    = PlayerPrefs.GetFloat("vol_sfx",    0.8f);
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- Volume controls ---
    public void SetMasterVolume(float linear) => SetDb("MasterVol", linear);
    public void SetMusicVolume (float linear) => SetDb("MusicVol",  linear);
    public void SetSFXVolume   (float linear) => SetDb("SFXVol",    linear);

    private void SetDb(string exposedParam, float linear)
    {
        if (!masterMixer)
        {
            Debug.LogWarning($"[AudioManager] Missing AudioMixer. Could not set '{exposedParam}'.", this);
            return;
        }

        float value = Mathf.Clamp01(linear);
        float dB = (value <= 0.0001f) ? -80f : Mathf.Log10(value) * 20f;
        masterMixer.SetFloat(exposedParam, dB);
    }

    // --- Playback ---
	public void TestPlay(SFX key) => PlaySFX(key);
    public void PlayUI() => PlaySFX(SFX.Click);
    public void PlaySFX(SFX key)
    {
        if (!sfxSource)
        {
            Debug.LogWarning("[AudioManager] Missing SFX AudioSource.", this);
            return;
        }

        if (sfxDict.TryGetValue(key, out var clip) && clip)
        {
            sfxSource.PlayOneShot(clip);
        }
        else if (TryResolveFallback(key, out clip) && clip)
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
        PlayMusic(clip, true);
    }

    public void PlayMusic(AudioClip clip, bool loop)
    {
        if (!clip || !musicSource) return;
        if (!musicSource.enabled)
        {
            musicSource.enabled = true;
        }

        if (!musicSource.gameObject.activeInHierarchy)
        {
            return;
        }

        musicSource.loop = loop;
        if (musicSource.isPlaying && musicSource.clip == clip)
        {
            return;
        }
        musicSource.clip = clip;
        musicSource.Play();
    }
    public void StopMusic()
    {
        if (musicSource)
        {
            musicSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (musicSource && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource && musicSource.clip)
        {
            musicSource.UnPause();
        }
    }

    private bool TryResolveFallback(SFX requested, out AudioClip clip)
    {
        clip = requested switch
        {
            SFX.Click => GetClipOrLoadResource(SFX.Click, "Audio/Click"),
            SFX.Cannon => GetClipOrLoadResource(SFX.Cannon, "Audio/CannonFire"),
            SFX.Impact => GetClipOrLoadResource(SFX.Impact, "Audio/Impact"),
            SFX.Ballista => GetClipOrLoadResource(SFX.Ballista, "Audio/Arrowfire"),
            SFX.PlaceTower => GetClipOrLoadResource(SFX.Click, "Audio/Click"),
            SFX.MagicFire => GetClipOrLoadResource(SFX.Ballista, "Audio/Arrowfire"),
            SFX.AirFire => GetClipOrLoadResource(SFX.Cannon, "Audio/CannonFire"),
            SFX.BoatHit => GetClipOrLoadResource(SFX.Impact, "Audio/Impact"),
            SFX.BoatDeath => GetClipOrLoadResource(SFX.Impact, "Audio/Impact"),
            SFX.Explosion => GetClipOrLoadResource(SFX.Explosion, "Audio/boat_explosion_placeholder") ?? GetClipOrLoadResource(SFX.Impact, "Audio/Impact"),
            SFX.WaterSplash => GetClipOrLoadResource(SFX.Impact, "Audio/Impact"),
            SFX.BaseHit => GetClipOrLoadResource(SFX.Impact, "Audio/Impact"),
            SFX.RoundStart => GetClipOrLoadResource(SFX.Click, "Audio/Click"),
            SFX.RoundComplete => GetClipOrLoadResource(SFX.Click, "Audio/Click"),
            SFX.BossSpawn => GetClipOrLoadResource(SFX.Cannon, "Audio/CannonFire"),
            _ => null
        };

        return clip != null;
    }

    private AudioClip GetClipOrNull(SFX key)
    {
        sfxDict.TryGetValue(key, out var clip);
        return clip;
    }

    private AudioClip GetClipOrLoadResource(SFX key, string resourcePath)
    {
        if (sfxDict.TryGetValue(key, out var existingClip) && existingClip)
        {
            return existingClip;
        }

        if (!allowPlaceholderResourceFallbacks || string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        AudioClip loadedClip = Resources.Load<AudioClip>(resourcePath);
        if (loadedClip)
        {
            sfxDict[key] = loadedClip;
        }

        return loadedClip;
    }

    private void EnsureAudioSources()
    {
        AudioSource[] existingSources = GetComponents<AudioSource>();
        musicSource = existingSources.Length > 0 ? existingSources[0] : gameObject.AddComponent<AudioSource>();
        sfxSource = existingSources.Length > 1 ? existingSources[1] : gameObject.AddComponent<AudioSource>();
        if (musicSource && !musicSource.enabled)
        {
            musicSource.enabled = true;
        }

        if (sfxSource && !sfxSource.enabled)
        {
            sfxSource.enabled = true;
        }
    }

    public void PlayMusicCue(MusicCue cue)
    {
        if (!musicSource)
        {
            Debug.LogWarning($"[AudioManager] Missing music AudioSource for cue '{cue}'.", this);
            return;
        }

        if (musicDict.TryGetValue(cue, out MusicEntry configuredEntry) && configuredEntry.clip)
        {
            PlayMusic(configuredEntry.clip, configuredEntry.loop);
            return;
        }

        string path = cue switch
        {
            MusicCue.MainMenu => mainMenuMusicResourcePath,
            MusicCue.GameplayBattle => gameplayBattleMusicResourcePath,
            MusicCue.GameplayBoss => gameplayBossMusicResourcePath,
            MusicCue.Victory => victoryMusicResourcePath,
            MusicCue.Defeat => defeatMusicResourcePath,
            _ => gameplayCalmMusicResourcePath,
        };

        AudioClip clip = string.IsNullOrWhiteSpace(path) ? null : Resources.Load<AudioClip>(path);
        if (clip)
        {
            bool loop = cue != MusicCue.Victory && cue != MusicCue.Defeat;
            PlayMusic(clip, loop);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] No music clip configured for cue '{cue}'.", this);
        }
    }
}
