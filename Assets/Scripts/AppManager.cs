using UnityEngine;
using UnityEngine.InputSystem; // 1. Require the New Input System namespace

public class AppManager : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject sfxNodePrefab;
    public Transform spawnPoint;
    public float maxSpawnRadius = 2.5f; // How far apart they spawn

    [Header("UI Reference")]
    public SFXUIController sharedUIController;

    [Header("Camera Reference")]
    public Camera mainCam; // 2. Drag your Orthographic Camera here in the Inspector

    private SFXNode currentlySelectedNode;

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main;

        // --- FORCE FMOD TO ROUTE BUSES ON STARTUP ---
        FMOD.Studio.Bus vocalBus = FMODUnity.RuntimeManager.GetBus("bus:/Vocal");
        vocalBus.lockChannelGroup();

        FMOD.Studio.Bus sfxBus = FMODUnity.RuntimeManager.GetBus("bus:/SFX");
        sfxBus.lockChannelGroup();
        // --------------------------------------------
    }

    void Update()
    {
        // 3. Safety check: Ensure a mouse is actually connected/detected
        if (Mouse.current == null) return;

        // 4. NEW INPUT SYSTEM: Check if left mouse button was clicked this frame
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 5. NEW INPUT SYSTEM: Read the 2D screen position of the mouse
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // Cast the ray using the explicit camera reference
            Ray ray = mainCam.ScreenPointToRay(mousePos);

            int layerMask = 1 << LayerMask.NameToLayer("SFXNode");

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                SFXNode clickedNode = hit.collider.GetComponent<SFXNode>();
                if (clickedNode != null)
                {
                    SelectNode(clickedNode);
                }
            }
        }
    }

    public void SpawnNewSFXNode()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("[AppManager] Spawn Point is missing! Please assign it in the Inspector.");
            return;
        }

        // 1. Generate a random distance and random direction in one line
        Vector2 randomScatter = Random.insideUnitCircle * maxSpawnRadius;

        // 2. Apply the random scatter to the X and Z axes, anchored exactly to your spawnPoint
        Vector3 finalSpawnPos = spawnPoint.position + new Vector3(randomScatter.x, 0f, randomScatter.y);

        // 3. Instantiate the prefab
        GameObject newNode = Instantiate(sfxNodePrefab, finalSpawnPos, Quaternion.identity);
    }

    private void SelectNode(SFXNode nodeToSelect)
    {
        currentlySelectedNode = nodeToSelect;
        sharedUIController.BindToNode(currentlySelectedNode);
    }
}