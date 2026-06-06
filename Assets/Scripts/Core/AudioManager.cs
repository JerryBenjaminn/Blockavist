using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string KeyMusic = "musicEnabled";
    private const string KeySFX   = "sfxEnabled";

    public bool MusicEnabled { get; private set; }
    public bool SFXEnabled   { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music")]
    [SerializeField] private AudioClip musicClip;

    [Header("SFX — UI")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip levelSelectClip;

    [Header("SFX — Cubby")]
    [SerializeField] private AudioClip cubbyDieClip;
    [SerializeField] private AudioClip cubbyGoalClip;

    [Header("SFX — Tiles")]
    [SerializeField] private AudioClip popTileClip;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-create AudioSources if not wired in the inspector
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop         = true;
            musicSource.playOnAwake  = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop        = false;
            sfxSource.playOnAwake = false;
        }

        MusicEnabled = PlayerPrefs.GetInt(KeyMusic, 1) == 1;
        SFXEnabled   = PlayerPrefs.GetInt(KeySFX,   1) == 1;
    }

    private void Start()
    {
        StartMusic();
    }

    private void StartMusic()
    {
        if (musicSource == null || musicClip == null) return;
        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.mute = !MusicEnabled;
        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    // ── Toggle API (called from SettingsUI) ───────────────────────────────────

    public void SetMusic(bool on)
    {
        MusicEnabled = on;
        PlayerPrefs.SetInt(KeyMusic, on ? 1 : 0);
        PlayerPrefs.Save();
        if (musicSource != null) musicSource.mute = !on;
    }

    public void SetSFX(bool on)
    {
        SFXEnabled = on;
        PlayerPrefs.SetInt(KeySFX, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ── SFX playback ──────────────────────────────────────────────────────────

    /// <summary>Play any clip through the shared SFX source. Respects the SFX toggle.</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (!SFXEnabled || sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayButtonClick()
    {
        if (buttonClickClip == null)
            Debug.LogWarning("[AudioManager] buttonClickClip is not assigned — assign it in the AudioManager inspector.");
        PlaySFX(buttonClickClip);
    }
    public void PlayLevelSelect() => PlaySFX(levelSelectClip);
    public void PlayCubbyDie()    => PlaySFX(cubbyDieClip);
    public void PlayCubbyGoal()   => PlaySFX(cubbyGoalClip);
    public void PlayPopTile()     => PlaySFX(popTileClip);
}
