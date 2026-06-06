using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Swaps the Image sprite between two states based on the parent Toggle's on/off value.
/// Used by the Settings toggles: check_square_green (ON) and check_square_red (OFF).
/// Wired by UIBuilder; the runtime listener is registered in Start.
/// </summary>
public class ToggleSpriteSwapper : MonoBehaviour
{
    [SerializeField] public Sprite onSprite;
    [SerializeField] public Sprite offSprite;
    [SerializeField] private Image image;

    private void Awake()
    {
        if (image == null) image = GetComponent<Image>();
    }

    private void Start()
    {
        var toggle = GetComponentInParent<Toggle>();
        if (toggle == null) return;
        Refresh(toggle.isOn);
        toggle.onValueChanged.AddListener(Refresh);
    }

    // Called when the panel re-activates — handles SetIsOnWithoutNotify from SettingsUI.OnEnable
    private void OnEnable()
    {
        var toggle = GetComponentInParent<Toggle>();
        if (toggle != null) Refresh(toggle.isOn);
    }

    public void Refresh(bool isOn)
    {
        if (image != null) image.sprite = isOn ? onSprite : offSprite;
    }
}
