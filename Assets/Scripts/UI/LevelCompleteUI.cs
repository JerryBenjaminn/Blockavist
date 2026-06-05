using UnityEngine;

public class LevelCompleteUI : MonoBehaviour
{
    public void OnNextLevelClicked()
    {
        gameObject.SetActive(false);
        GameManager.Instance?.LoadNextLevel();
    }

    public void OnWorldSelectClicked() => UIManager.Instance?.GoToWorldSelect();
}
