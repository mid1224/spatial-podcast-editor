using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleFileBrowser;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        TopDown2D,
        Fly3D
    }

    [Header("Mode")]
    public CameraMode currentMode = CameraMode.TopDown2D;
    public KeyCode toggleModeHotkey = KeyCode.Tab;
    public Button toggleModeButton;

    [Header("Toggle Button Text")]
    public string topDownButtonText = "Switch to Edit View";
    public string fly3DButtonText = "Switch to Fly View";

    [Header("Top-Down 2D Settings")]
    public float panSpeed = 15f;
    public float dragSpeed = 20f;
    public float zoomSpeed = 50f;
    public float minZoom = 2f;
    public float maxZoom = 25f;

    [Header("Flycam 3D Settings")]
    public float flyMoveSpeed = 10f;
    public float flySprintMultiplier = 2.5f;
    public float flyLookSensitivity = 2f;
    public float flyVerticalSpeed = 8f;
    public float flyZoomSpeed = 120f;
    public float minFov = 25f;
    public float maxFov = 90f;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    [Header("Flycam Reset State")]
    public Vector3 flyResetPosition = new Vector3(0f, 8f, -10f);
    public Vector3 flyResetEulerAngles = new Vector3(25f, 0f, 0f);
    public float flyResetFov = 60f;

    [Header("Controls")]
    public KeyCode resetHotkey = KeyCode.R;
    public Button resetButton;

    private static readonly Vector3 TopDownFixedEuler = new Vector3(90f, 0f, 0f);

    private Vector3 topDownResetPosition;
    private float topDownResetZoom;
    private Camera cam;
    private float pitch;

    private TMP_Text toggleModeButtonTmpText;
    private Text toggleModeButtonText;

    void Start()
    {
        cam = GetComponent<Camera>();

        resetButton.onClick.AddListener(ResetCamera);
        toggleModeButton.onClick.AddListener(ToggleMode);

        toggleModeButtonTmpText = toggleModeButton.GetComponentInChildren<TMP_Text>(true);
        if (toggleModeButtonTmpText == null)
        {
            toggleModeButtonText = toggleModeButton.GetComponentInChildren<Text>(true);
        }

        topDownResetPosition = transform.position;
        topDownResetZoom = cam.orthographicSize;

        pitch = transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        ApplyModeSettings(currentMode);
        ResetCamera();
        UpdateToggleModeButtonText();
    }

    void Update()
    {
        if (FileBrowser.IsOpen)
        {
            return;
        }

        if (Input.GetKeyDown(toggleModeHotkey))
        {
            ToggleMode();
        }

        if (Input.GetKeyDown(resetHotkey))
        {
            ResetCamera();
        }

        if (currentMode == CameraMode.TopDown2D)
        {
            ApplyTopDownFixedRotation();
            HandleKeyboardPanning();
            HandleMouseDragging();
            HandleTopDownZooming();
        }
        else
        {
            HandleFlycamLook();
            HandleFlycamMovement();
            HandleFlycamZooming();
        }
    }

    public void SetTopDownMode()
    {
        currentMode = CameraMode.TopDown2D;
        ApplyModeSettings(currentMode);
        UpdateToggleModeButtonText();
    }

    public void SetFlycamMode()
    {
        currentMode = CameraMode.Fly3D;
        ApplyModeSettings(currentMode);
        UpdateToggleModeButtonText();
    }

    public void ToggleMode()
    {
        currentMode = currentMode == CameraMode.TopDown2D ? CameraMode.Fly3D : CameraMode.TopDown2D;
        ApplyModeSettings(currentMode);
        ResetCamera();
        UpdateToggleModeButtonText();
    }

    private void UpdateToggleModeButtonText()
    {
        string text = currentMode == CameraMode.TopDown2D ? fly3DButtonText : topDownButtonText;

        if (toggleModeButtonTmpText != null)
        {
            toggleModeButtonTmpText.text = text;
        }
        else if (toggleModeButtonText != null)
        {
            toggleModeButtonText.text = text;
        }
    }

    private void ApplyModeSettings(CameraMode mode)
    {
        if (mode == CameraMode.TopDown2D)
        {
            cam.orthographic = true;
            ApplyTopDownFixedRotation();
        }
        else
        {
            cam.orthographic = false;
            pitch = transform.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
        }
    }

    private void ApplyTopDownFixedRotation()
    {
        transform.rotation = Quaternion.Euler(TopDownFixedEuler);
    }

    private void HandleKeyboardPanning()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h != 0f || v != 0f)
        {
            float dynamicPanSpeed = panSpeed * (cam.orthographicSize / 10f);
            Vector3 move = new Vector3(h, 0f, v).normalized;
            transform.Translate(move * dynamicPanSpeed * Time.deltaTime, Space.World);
        }
    }

    private void HandleMouseDragging()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = -Input.GetAxis("Mouse X");
            float mouseY = -Input.GetAxis("Mouse Y");

            float dynamicDragSpeed = dragSpeed * (cam.orthographicSize / 10f);
            Vector3 dragMove = new Vector3(mouseX, 0f, mouseY);
            transform.Translate(dragMove * dynamicDragSpeed * Time.deltaTime, Space.World);
        }
    }

    private void HandleTopDownZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f) return;

        cam.orthographicSize -= scroll * zoomSpeed * Time.deltaTime;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }

    private void HandleFlycamLook()
    {
        if (!Input.GetMouseButton(1)) return;

        float mouseX = Input.GetAxis("Mouse X") * flyLookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * flyLookSensitivity;

        float yaw = transform.eulerAngles.y + mouseX;
        pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleFlycamMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float upDown = 0f;
        if (Input.GetKey(KeyCode.E)) upDown += 1f;
        if (Input.GetKey(KeyCode.Q)) upDown -= 1f;

        Vector3 move =
            (transform.right * h) +
            (transform.forward * v) +
            (Vector3.up * upDown);

        if (move.sqrMagnitude > 1f) move.Normalize();

        float speed = flyMoveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            speed *= flySprintMultiplier;
        }

        transform.position += move * speed * Time.deltaTime;

        if (upDown != 0f)
        {
            transform.position += Vector3.up * upDown * flyVerticalSpeed * Time.deltaTime;
        }
    }

    private void HandleFlycamZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f) return;

        cam.fieldOfView -= scroll * flyZoomSpeed * Time.deltaTime;
        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minFov, maxFov);
    }

    public void ResetCamera()
    {
        if (currentMode == CameraMode.TopDown2D)
        {
            transform.position = topDownResetPosition;
            ApplyTopDownFixedRotation();
            cam.orthographicSize = topDownResetZoom;
        }
        else
        {
            transform.position = flyResetPosition;
            transform.rotation = Quaternion.Euler(flyResetEulerAngles);
            cam.fieldOfView = flyResetFov;

            pitch = transform.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
        }
    }
}