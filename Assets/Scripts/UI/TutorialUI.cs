using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a single tutorial pop-up and invokes a callback when confirmed.
/// TutorialManager owns the queue and calls Show() once per step.
/// Inspector refs (messageText, buttonLabel, confirmButton) are wired by UIBuilder.
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private Button   confirmButton;

    private Action _onDone;

    public void Show(string message, string btnLabel, Action onDone)
    {
        _onDone = onDone;
        if (messageText != null) messageText.text = message;
        if (buttonLabel  != null) buttonLabel.text  = btnLabel;
        gameObject.SetActive(true);
    }

    // Wired to the confirm button's OnClick in the Inspector (via UIBuilder).
    public void OnConfirmClicked()
    {
        gameObject.SetActive(false);
        _onDone?.Invoke();
    }
}
