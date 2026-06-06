using UnityEngine;

// Drives the idle up-down bob on Cubby's Visual child via Mathf.Sin.
// PlayerController disables this component on death and goal.
public class CubbyBob : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.07f;
    [SerializeField] private float speed     = 1.8f;

    private Vector3 basePosition;

    private void OnEnable()
    {
        basePosition = transform.localPosition;
    }

    private void Update()
    {
        float y = Mathf.Sin(Time.time * speed * Mathf.PI * 2f) * amplitude;
        transform.localPosition = basePosition + new Vector3(0f, y, 0f);
    }

    private void OnDisable()
    {
        transform.localPosition = basePosition;
    }
}
