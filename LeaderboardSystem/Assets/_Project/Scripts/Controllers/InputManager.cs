using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera eventCamera;
    [SerializeField] private string buttonTag = "UpdateButton";
    [SerializeField] private LeaderboardController controller;

    void Awake()
    {
        if (eventCamera == null) eventCamera = Camera.main;
    }

    void Update()
    {
        // mouse sol týk
        if (Input.GetMouseButtonDown(0))
            TryRaycast(Input.mousePosition);

        // dokunma
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TryRaycast(Input.GetTouch(0).position);
    }

    // touch detection
    private void TryRaycast(Vector3 screenPos)
    {
        if (eventCamera == null) return;

        var ray = eventCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, 1000f)
            && hit.collider != null
            && hit.collider.CompareTag(buttonTag)
            && controller != null)
        {
            controller.SimulateRandomUpdateAnimated();
        }
    }
}
