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
        // Mouse
        if (Input.GetMouseButtonDown(0))
            TryRaycast(Input.mousePosition);

        // Touch
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TryRaycast(Input.GetTouch(0).position);
    }

    private void TryRaycast(Vector3 screenPos)
    {
        if (eventCamera == null) return;

        Ray ray = eventCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            if (hit.collider != null && hit.collider.CompareTag(buttonTag))
            {
                // Direkt controller içindeki fonksiyonu çaðýr
                if (controller != null)
                    controller.SimulateRandomUpdateAnimated();
            }
        }
    }
}
