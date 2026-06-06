using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays the button-click SFX whenever this button is pressed.
/// Added automatically to every standard UI button by UIBuilder.EnsureButtonSFX().
/// Level-select grid buttons are excluded — they call AudioManager directly in OnClicked().
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
        {
            // Awake hasn't run yet — fetch it now as a fallback
            button = GetComponent<Button>();
        }

        Debug.Log($"[ButtonSFX] OnEnable — '{name}'  button={(button != null ? "found" : "MISSING")}  AudioManager={(AudioManager.Instance != null ? "found" : "null — not yet initialised")}");

        if (button != null)
            button.onClick.AddListener(Play);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(Play);
    }

    private void Play()
    {
        Debug.Log($"[ButtonSFX] Play — '{name}'  AudioManager={(AudioManager.Instance != null ? "found" : "NULL")}  SFXEnabled={(AudioManager.Instance?.SFXEnabled.ToString() ?? "n/a")}");
        AudioManager.Instance?.PlayButtonClick();
    }
}
