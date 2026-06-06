using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    // Button callbacks — wired in UIBuilder via Button.onClick
    public void OnPlayClicked()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[MainMenuUI] UIManager.Instance is null — is UIManager in the scene?");
            return;
        }
        UIManager.Instance.GoToWorldSelect();
    }

    public void OnSettingsClicked()
    {
        UIManager.Instance?.ShowSettings();
    }
}
