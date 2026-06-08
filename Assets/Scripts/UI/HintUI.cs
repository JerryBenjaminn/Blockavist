using UnityEngine;

public class HintUI : MonoBehaviour
{
    public void OnCloseClicked() => UIManager.Instance?.HideHint();
}
