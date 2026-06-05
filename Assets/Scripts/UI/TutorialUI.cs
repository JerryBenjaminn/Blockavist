using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays sequential tutorial pop-ups and invokes a callback when all are confirmed.
/// Wire messageText, buttonLabel, and confirmButton in the Inspector.
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text buttonLabel;
    [SerializeField] private Button   confirmButton;

    private static readonly string[] Messages = {
        "Tap yellow blocks to destroy them",
        "Guide Cubby to the green goal!"
    };

    private static readonly string[] ButtonLabels = {
        "Got it!",
        "Let's go!"
    };

    private int    _step;
    private Action _onAllDone;

    public void Show(Action onAllDone)
    {
        Debug.Log($"[TutorialUI] Show() called — activating panel. messageText={(messageText != null ? "OK" : "NULL")}  buttonLabel={(buttonLabel != null ? "OK" : "NULL")}  confirmButton={(confirmButton != null ? "OK" : "NULL")}");
        _onAllDone = onAllDone;
        _step      = 0;
        gameObject.SetActive(true);
        ApplyStep();
    }

    // Wired to the confirm button's OnClick in the Inspector.
    public void OnConfirmClicked()
    {
        _step++;
        bool done = _step >= Messages.Length;
        Debug.Log($"[TutorialUI] OnConfirmClicked — step now={_step}  done={done}  onAllDone={((_onAllDone != null) ? "set" : "NULL")}");
        if (!done)
        {
            ApplyStep();
        }
        else
        {
            gameObject.SetActive(false);
            _onAllDone?.Invoke();
        }
    }

    private void ApplyStep()
    {
        if (messageText != null) messageText.text = Messages[_step];
        if (buttonLabel  != null) buttonLabel.text  = ButtonLabels[_step];
    }
}
