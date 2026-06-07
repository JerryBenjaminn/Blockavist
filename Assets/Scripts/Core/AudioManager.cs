using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string KeyMusic = "MusicEnabled";
    private const string KeySFX   = "SFXEnabled";

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

        if (musicSource == null)
        {
            musicSource             = gameObject.AddComponent<AudioSource>();
            musicSource.loop        = true;
            musicSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource             = gameObject.AddComponent<AudioSource>();
            sfxSource.loop        = false;
            sfxSource.playOnAwake = false;
        }

        MusicEnabled = PlayerPrefs.GetInt(KeyMusic, 1) == 1;
        SFXEnabled   = PlayerPrefs.GetInt(KeySFX,   1) == 1;
    }

    private void Start()
    {
        if (musicSource != null) musicSource.mute = !MusicEnabled;
        if (sfxSource   != null) sfxSource.mute   = !SFXEnabled;

        if (MusicEnabled && musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    // ── Toggle API (called from SettingsUI) ───────────────────────────────────

    public void SetMusicEnabled(bool on)
    {
        MusicEnabled = on;
        if (musicSource != null)
        {
            musicSource.mute = !on;
            if (on && musicClip != null && !musicSource.isPlaying)
            {
                if (musicSource.clip == null) musicSource.clip = musicClip;
                musicSource.Play();
            }
        }
        PlayerPrefs.SetInt(KeyMusic, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSFXEnabled(bool on)
    {
        SFXEnabled = on;
        if (sfxSource != null) sfxSource.mute = !on;
        PlayerPrefs.SetInt(KeySFX, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ── SFX playback ──────────────────────────────────────────────────────────

    public void PlaySFX(AudioClip clip)
    {
        if (!SFXEnabled || sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayButtonClick() => PlaySFX(buttonClickClip);
    public void PlayLevelSelect() => PlaySFX(levelSelectClip);
    public void PlayCubbyDie()    => PlaySFX(cubbyDieClip);
    public void PlayCubbyGoal()   => PlaySFX(cubbyGoalClip);
    public void PlayPopTile()     => PlaySFX(popTileClip);
}
