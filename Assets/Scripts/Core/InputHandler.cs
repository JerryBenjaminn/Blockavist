using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Handles all player input via the new Input System.
/// Works for both mouse (editor/PC) and multi-touch (mobile) without legacy Input.
/// </summary>
public class InputHandler : MonoBehaviour
{
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        if (mainCamera == null) return;

        bool gameIsOver = GameManager.Instance != null &&
                          GameManager.Instance.CurrentState != GameManager.GameState.Playing;

        if (gameIsOver)
        {
            if (AnyTapThisFrame())
                GameManager.Instance.RestartLevel();
            return;
        }

        // Multi-touch (mobile)
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                HandleTap(touch.screenPosition);
        }

        // Mouse fallback (editor / PC); skip if touch is already being handled
        // to avoid double-firing on touch-enabled screens that also emit mouse events.
        if (Touch.activeTouches.Count == 0 &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleTap(Mouse.current.position.ReadValue());
        }
    }

    private bool AnyTapThisFrame()
    {
        foreach (var touch in Touch.activeTouches)
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                return true;

        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private void HandleTap(Vector2 screenPosition)
    {
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider == null) return;

        TileElement tile = hit.collider.GetComponent<TileElement>();
        if (tile != null && tile.IsDestructible)
            tile.OnPlayerTouch();
    }
}
