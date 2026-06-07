using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Sprite toggleOnSprite;
    [SerializeField] private Sprite toggleOffSprite;

    private void Awake()
    {
        musicToggle.onValueChanged.AddListener(OnMusicToggled);
        sfxToggle  .onValueChanged.AddListener(OnSFXToggled);
    }

    private void OnEnable()
    {
        if (AudioManager.Instance == null) return;
        ApplyToggleState(musicToggle, AudioManager.Instance.MusicEnabled);
        ApplyToggleState(sfxToggle,   AudioManager.Instance.SFXEnabled);
    }

    private void ApplyToggleState(Toggle toggle, bool on)
    {
        toggle.SetIsOnWithoutNotify(on);
        var img = toggle.targetGraphic as Image;
        if (img != null) img.sprite = on ? toggleOnSprite : toggleOffSprite;
    }

    private void OnMusicToggled(bool on)
    {
        AudioManager.Instance?.SetMusicEnabled(on);
        var img = musicToggle.targetGraphic as Image;
        if (img != null) img.sprite = on ? toggleOnSprite : toggleOffSprite;
    }

    private void OnSFXToggled(bool on)
    {
        AudioManager.Instance?.SetSFXEnabled(on);
        var img = sfxToggle.targetGraphic as Image;
        if (img != null) img.sprite = on ? toggleOnSprite : toggleOffSprite;
    }

    public void OnCloseClicked() => UIManager.Instance?.HideSettings();
}
