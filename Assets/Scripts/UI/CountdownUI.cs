using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownUI : MonoBehaviour
{
    // Fired after "GO!" and player unfreeze — TutorialManager listens to this.
    public static event Action OnComplete;

    [SerializeField] private TMP_Text countdownText;

    // Called by UIManager after the level is loaded and the scene has faded in.
    public void Begin()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // Freeze player immediately (player may still be falling to floor — gravity stays active)
        var player = LevelManager.Instance?.ActivePlayer;
        player?.Freeze();
        player?.ShowPeaceSign();

        string[] steps    = { "3", "2", "1", "GO!" };
        float[]  durations = { 0.9f, 0.9f, 0.9f, 0.5f };

        for (int i = 0; i < steps.Length; i++)
        {
            if (countdownText != null) countdownText.text = steps[i];
            yield return new WaitForSeconds(durations[i]);
        }

        // Unfreeze — only if still the same player (could be null if level was aborted)
        LevelManager.Instance?.ActivePlayer?.HidePeaceSign();
        LevelManager.Instance?.ActivePlayer?.Unfreeze();

        if (countdownText != null) countdownText.text = string.Empty;

        // Fire before SetActive(false): disabling the GO stops the coroutine immediately,
        // so any code after SetActive(false) is never reached.
        OnComplete?.Invoke();

        gameObject.SetActive(false);
    }
}
