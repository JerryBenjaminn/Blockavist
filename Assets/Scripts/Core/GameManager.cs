using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, GameOver, LevelComplete }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void OnPlayerDied()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.GameOver;
        Debug.Log("[Blockavist] Game Over — tap to restart.");
        // TODO Phase 3: show Game Over UI
    }

    public void OnLevelComplete()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.LevelComplete;
        Debug.Log("[Blockavist] Level Complete!");
        // TODO Phase 3: show Level Complete UI, unlock next level
    }

    public void RestartLevel()
    {
        CurrentState = GameState.Playing;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
