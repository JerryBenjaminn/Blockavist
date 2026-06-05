using TMPro;
using UnityEngine;

public class LevelSelectUI : MonoBehaviour
{
    [SerializeField] private TMP_Text       titleText;
    [SerializeField] private LevelButtonUI[] levelButtons;  // 10 buttons

    private int  currentWorld;
    private bool _initialized;

    public void Setup(int world)
    {
        _initialized = true;
        currentWorld = world;
        if (titleText != null)
            titleText.text = $"World {world}";

        int offset = (world - 1) * 10;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null) continue;

            int levelNumber = offset + i + 1;
            bool unlocked   = ProgressManager.Instance?.IsLevelUnlocked(levelNumber) ?? (i == 0);
            int  idx        = GameManager.Instance?.FindLevelIndex(levelNumber, world) ?? -1;

            levelButtons[i].Setup(levelNumber, unlocked, idx);
        }
    }

    // Only refresh on re-enable after Setup() has been called at least once.
    // Guarding here prevents SetActive(true) during ShowOnlyScreen from firing
    // Setup() before UIManager's midpoint callback sets the correct world — which
    // would toggle TMP_Text children inside a GridLayoutGroup before their own
    // OnEnable runs, triggering a recursive layout-rebuild cascade in Unity's UI system.
    private void OnEnable()
    {
        if (_initialized) Setup(currentWorld);
    }

    public void OnBackClicked() => UIManager.Instance?.GoToWorldSelect();
}
