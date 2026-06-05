using UnityEngine;

/// <summary>
/// Manages music and SFX toggles, persisted in PlayerPrefs.
/// Hook AudioSources up in Phase 5; toggles already track state correctly.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string KeyMusic = "musicEnabled";
    private const string KeySFX   = "sfxEnabled";

    public bool MusicEnabled { get; private set; }
    public bool SFXEnabled   { get; private set; }

    [SerializeField] private AudioSource musicSource;  // assign in inspector (Phase 5)

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        MusicEnabled = PlayerPrefs.GetInt(KeyMusic, 1) == 1;
        SFXEnabled   = PlayerPrefs.GetInt(KeySFX,   1) == 1;

        ApplyMusic();
    }

    public void SetMusic(bool on)
    {
        MusicEnabled = on;
        PlayerPrefs.SetInt(KeyMusic, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusic();
    }

    public void SetSFX(bool on)
    {
        SFXEnabled = on;
        PlayerPrefs.SetInt(KeySFX, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyMusic()
    {
        if (musicSource != null)
            musicSource.mute = !MusicEnabled;
    }
}
