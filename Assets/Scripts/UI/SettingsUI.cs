using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;

    private void OnEnable()
    {
        // Sync toggles to saved state without triggering listeners
        if (AudioManager.Instance == null) return;
        musicToggle.SetIsOnWithoutNotify(AudioManager.Instance.MusicEnabled);
        sfxToggle  .SetIsOnWithoutNotify(AudioManager.Instance.SFXEnabled);
    }

    // Wired via Toggle.onValueChanged in UIBuilder
    public void OnMusicToggled(bool on) => AudioManager.Instance?.SetMusic(on);
    public void OnSFXToggled  (bool on) => AudioManager.Instance?.SetSFX(on);

    public void OnCloseClicked() => UIManager.Instance?.HideSettings();
}
