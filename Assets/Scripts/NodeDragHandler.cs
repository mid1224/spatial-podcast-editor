using UnityEngine;
using UnityEngine.InputSystem; // Required for New Input System

public class NodeDragHandler : MonoBehaviour
{
    private Camera mainCam;
    private bool isDragging = false;
    private float fixedYPosition;

    void Start()
    {
        // Find the camera in the scene safely
        mainCam = Camera.main;
        fixedYPosition = transform.position.y; // Keep the node locked to its original floor height
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // 1. Detect Click Start
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCam.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // If the raycast hits THIS specific node's collider, start dragging
                if (hit.transform == transform)
                {
                    isDragging = true;
                }
            }
        }

        // 2. Handle Dragging State
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // Calculate distance from orthographic camera plane to the floor
            float distanceToPlane = mainCam.transform.position.y - fixedYPosition;
            Vector3 screenPos = new Vector3(mousePos.x, mousePos.y, distanceToPlane);

            Vector3 worldPos = mainCam.ScreenToWorldPoint(screenPos);

            // Move the node, keeping the Y coordinate perfectly flat
            transform.position = new Vector3(worldPos.x, fixedYPosition, worldPos.z);
        }

        // 3. Detect Click Release
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }
}