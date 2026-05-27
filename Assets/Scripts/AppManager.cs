using UnityEngine;
using UnityEngine.InputSystem;

public class AppManager : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject sfxNodePrefab;
    public Transform spawnPoint;
    public float maxSpawnRadius = 2.5f; // How far apart they spawn

    [Header("UI Reference")]
    public SFXUIController sharedUIController;

    [Header("Camera Reference")]
    public Camera mainCam;

    private SFXNode currentlySelectedSfxNode;
    private VocalNode currentlySelectedVocalNode;

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main;

        FMODUnity.RuntimeManager.CoreSystem.set3DSettings(1.0f, 1.0f, 1.0f);

        FMOD.Studio.Bus vocalBus = FMODUnity.RuntimeManager.GetBus("bus:/Vocal");
        vocalBus.lockChannelGroup();

        FMOD.Studio.Bus sfxBus = FMODUnity.RuntimeManager.GetBus("bus:/SFX");
        sfxBus.lockChannelGroup();
    }

    void Update()
    {
        if (Mouse.current == null || mainCam == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCam.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                SFXNode clickedSfx = hit.collider.GetComponentInParent<SFXNode>();
                if (clickedSfx != null)
                {
                    SelectSfxNode(clickedSfx);
                    return;
                }

                VocalNode clickedVocal = hit.collider.GetComponentInParent<VocalNode>();
                if (clickedVocal != null)
                {
                    SelectVocalNode(clickedVocal);
                    return;
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

        Vector2 randomScatter = Random.insideUnitCircle * maxSpawnRadius;
        Vector3 finalSpawnPos = spawnPoint.position + new Vector3(randomScatter.x, 0f, randomScatter.y);

        Instantiate(sfxNodePrefab, finalSpawnPos, Quaternion.identity);
    }

    private void SelectSfxNode(SFXNode nodeToSelect)
    {
        if (currentlySelectedVocalNode != null)
        {
            currentlySelectedVocalNode.SetSelected(false);
            currentlySelectedVocalNode = null;
        }

        if (currentlySelectedSfxNode != null && currentlySelectedSfxNode != nodeToSelect)
        {
            currentlySelectedSfxNode.SetSelected(false);
        }

        currentlySelectedSfxNode = nodeToSelect;
        currentlySelectedSfxNode.SetSelected(true);

        if (sharedUIController != null)
        {
            sharedUIController.BindToNode(currentlySelectedSfxNode);
        }
    }

    private void SelectVocalNode(VocalNode nodeToSelect)
    {
        if (currentlySelectedSfxNode != null)
        {
            currentlySelectedSfxNode.SetSelected(false);
            currentlySelectedSfxNode = null;
        }

        if (currentlySelectedVocalNode != null && currentlySelectedVocalNode != nodeToSelect)
        {
            currentlySelectedVocalNode.SetSelected(false);
        }

        currentlySelectedVocalNode = nodeToSelect;
        currentlySelectedVocalNode.SetSelected(true);

        // Hide SFX UI when vocal node is selected
        if (sharedUIController != null)
        {
            sharedUIController.gameObject.SetActive(false);
        }
    }
}