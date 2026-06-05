using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    [SerializeField] private Image      progressFill;
    [SerializeField] private TMP_Text   statusText;
    [SerializeField] private float      duration = 1.8f;

    public void Begin(Action onComplete)
    {
        gameObject.SetActive(true);
        StartCoroutine(LoadRoutine(onComplete));
    }

    private IEnumerator LoadRoutine(Action onComplete)
    {
        if (progressFill != null) progressFill.fillAmount = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (progressFill != null) progressFill.fillAmount = t;
            if (statusText   != null) statusText.text = $"Loading… {(int)(t * 100)}%";
            yield return null;
        }

        if (progressFill != null) progressFill.fillAmount = 1f;
        if (statusText   != null) statusText.text = "Ready!";

        yield return new WaitForSeconds(0.25f);
        onComplete?.Invoke();
    }
}
