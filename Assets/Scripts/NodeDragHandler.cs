using UnityEngine;
using UnityEngine.InputSystem; // Required for New Input System

public class NodeDragHandler : MonoBehaviour
{
    [Header("Drag Limit")]
    public float maxDragRadius = 20f;

    public Vector3 dragCenter;

    private Camera mainCam;
    private CameraController cameraController;
    public bool isDragging = false; // Changed to public so nodes can read it
    private float fixedYPosition;   

    void Start()
    {
        // Find the camera in the scene safely
        mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraController = mainCam.GetComponent<CameraController>();
        }

        fixedYPosition = transform.position.y; // Keep the node locked to its original floor height
    }

    void Update()
    {
        if (Mouse.current == null || mainCam == null) return;

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
            Ray ray = mainCam.ScreenPointToRay(mousePos);
            Plane floorPlane = new Plane(Vector3.up, new Vector3(0, fixedYPosition, 0));

            if (floorPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPos = ray.GetPoint(distance);
                Vector3 targetPos = new Vector3(worldPos.x, fixedYPosition, worldPos.z);

                // Clamp to max drag radius (XZ plane)
                if (maxDragRadius > 0f)
                {
                    Vector3 offset = targetPos - dragCenter;
                    offset.y = 0f;

                    if (offset.magnitude > maxDragRadius)
                    {
                        offset = offset.normalized * maxDragRadius;
                    }

                    targetPos = dragCenter + offset;
                    targetPos.y = fixedYPosition;
                }

                transform.position = targetPos;
            }
        }

        // 3. Detect Click Release
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }
}