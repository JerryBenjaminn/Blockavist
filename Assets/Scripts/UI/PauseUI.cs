using UnityEngine;

public class PauseUI : MonoBehaviour
{
    public void OnResumeClicked()      => UIManager.Instance?.HidePause();
    public void OnWorldSelectClicked() => UIManager.Instance?.GoToWorldSelect();
}
