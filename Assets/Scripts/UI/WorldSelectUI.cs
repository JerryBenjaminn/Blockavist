using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldSelectUI : MonoBehaviour
{
    [SerializeField] private Button     world1Button;
    [SerializeField] private Button     world2Button;
    [SerializeField] private GameObject world2LockIcon;
    [SerializeField] private TMP_Text   world2SubText;
    [SerializeField] private Button     world3Button;
    [SerializeField] private GameObject world3LockIcon;
    [SerializeField] private TMP_Text   world3SubText;

    public void Refresh()
    {
        bool w2 = ProgressManager.Instance != null && ProgressManager.Instance.IsWorldUnlocked(2);
        if (world2Button   != null) world2Button.interactable = w2;
        if (world2LockIcon != null) world2LockIcon.SetActive(!w2);
        if (world2SubText  != null)
            world2SubText.text = w2 ? "Levels 11–20" : "Complete World 1 to unlock";

        bool w3 = ProgressManager.Instance != null && ProgressManager.Instance.IsWorldUnlocked(3);
        if (world3Button   != null) world3Button.interactable = w3;
        if (world3LockIcon != null) world3LockIcon.SetActive(!w3);
        if (world3SubText  != null)
            world3SubText.text = w3 ? "Levels 21–30" : "Complete World 2 to unlock";
    }

    public void OnWorld1Clicked() => UIManager.Instance?.GoToLevelSelect(1);

    public void OnWorld2Clicked()
    {
        if (ProgressManager.Instance != null && ProgressManager.Instance.IsWorldUnlocked(2))
            UIManager.Instance?.GoToLevelSelect(2);
    }

    public void OnWorld3Clicked()
    {
        if (ProgressManager.Instance != null && ProgressManager.Instance.IsWorldUnlocked(3))
            UIManager.Instance?.GoToLevelSelect(3);
    }

    public void OnBackClicked() => UIManager.Instance?.GoToMainMenu();
}
