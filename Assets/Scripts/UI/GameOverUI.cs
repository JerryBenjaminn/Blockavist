using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    public void OnRetryClicked()
    {
        gameObject.SetActive(false);
        GameManager.Instance?.RestartLevel();
    }

    public void OnWorldSelectClicked() => UIManager.Instance?.GoToWorldSelect();
}
