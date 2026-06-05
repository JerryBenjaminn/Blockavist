using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    // Button callbacks — wired in UIBuilder via Button.onClick
    public void OnPlayClicked()
    {
        UIManager.Instance?.GoToWorldSelect();
    }

    public void OnSettingsClicked()
    {
        UIManager.Instance?.ShowSettings();
    }
}
