using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One button in the level-select grid.</summary>
public class LevelButtonUI : MonoBehaviour
{
    [SerializeField] private Button     button;
    [SerializeField] private TMP_Text   numberText;
    [SerializeField] private GameObject lockIcon;

    private int levelIndex = -1;

    public void Setup(int levelNumber, bool unlocked, int index)
    {
        levelIndex = index;
        if (numberText != null) numberText.text = levelNumber.ToString();
        if (lockIcon   != null) lockIcon.SetActive(!unlocked);
        if (button     != null) button.interactable = unlocked && index >= 0;
    }

    // Wired via Button.onClick in UIBuilder
    public void OnClicked()
    {
        if (levelIndex < 0) return;
        UIManager.Instance?.GoToGame(levelIndex);
    }
}
