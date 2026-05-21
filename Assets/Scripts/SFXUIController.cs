using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleFileBrowser; // Ensure this is added

public class SFXUIController : MonoBehaviour
{
    [Header("UI Controls")]
    public Button playButton;
    public Button stopButton;
    public Button recordButton; // Added Record Button
    public Toggle loopToggle;
    public Slider volumeSlider;
    public TextMeshProUGUI statusText;
    public Button deleteButton;

    [Header("Color Sliders (RGB)")]
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;

    [Header("Height Control")]
    public Slider heightSlider;
    public float heightMin = -10f;
    public float heightMax = 10f;

    [Header("Name / Settings UI")]
    public TMP_InputField nameInput; // editable name field in settings (also displays the current name)

    [Header("Audio Loading")]
    public TMP_Text sfxPathText; // Changed to TMP_Text to just display the name
    public Button loadSfxButton;

    private SFXNode activeNode;

    void Start()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        stopButton.onClick.AddListener(OnStopClicked);
        
        if (recordButton != null)
        {
            recordButton.onClick.AddListener(OnRecordClicked);
        }
        
        loopToggle.onValueChanged.AddListener(OnLoopChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        loadSfxButton.onClick.AddListener(OnLoadSfxClicked);
        deleteButton.onClick.AddListener(OnDeleteNodeClicked);

        // Configure color sliders to use 0..255 whole number ranges and wire listeners
        if (redSlider != null)
        {
            redSlider.minValue = 0f;
            redSlider.maxValue = 255f;
            redSlider.wholeNumbers = true;
            redSlider.onValueChanged.AddListener(OnColorSliderChanged);
        }
        if (greenSlider != null)
        {
            greenSlider.minValue = 0f;
            greenSlider.maxValue = 255f;
            greenSlider.wholeNumbers = true;
            greenSlider.onValueChanged.AddListener(OnColorSliderChanged);
        }
        if (blueSlider != null)
        {
            blueSlider.minValue = 0f;
            blueSlider.maxValue = 255f;
            blueSlider.wholeNumbers = true;
            blueSlider.onValueChanged.AddListener(OnColorSliderChanged);
        }

        // Configure height slider
        if (heightSlider != null)
        {
            heightSlider.minValue = heightMin;
            heightSlider.maxValue = heightMax;
            heightSlider.onValueChanged.AddListener(OnHeightChanged);
        }

        // Name input listener
        if (nameInput != null)
        {
            nameInput.onEndEdit.AddListener(OnNameInputEndEdit);
        }

        gameObject.SetActive(false);
    }

    public void BindToNode(SFXNode node)
    {
        activeNode = node;
        gameObject.SetActive(true);

        // Show saved audio filename (trimmed from absolute path) in the UI Text
        if (sfxPathText != null)
        {
            sfxPathText.text = string.IsNullOrEmpty(activeNode.audioPath) ? "No File Loaded" : Path.GetFileName(activeNode.audioPath);
        }

        loopToggle.SetIsOnWithoutNotify(activeNode.isLooping);
        volumeSlider.SetValueWithoutNotify(activeNode.volume);
        activeNode.SetIcon(activeNode.isPlaying);

        // Initialize color sliders from node (convert 0..1 -> 0..255)
        if (redSlider != null && greenSlider != null && blueSlider != null)
        {
            float r = Mathf.Clamp01(activeNode.iconColor.r);
            float g = Mathf.Clamp01(activeNode.iconColor.g);
            float b = Mathf.Clamp01(activeNode.iconColor.b);

            redSlider.SetValueWithoutNotify(Mathf.Round(r * 255f));
            greenSlider.SetValueWithoutNotify(Mathf.Round(g * 255f));
            blueSlider.SetValueWithoutNotify(Mathf.Round(b * 255f));
        }

        // Initialize height slider from node
        if (heightSlider != null)
        {
            heightSlider.SetValueWithoutNotify(activeNode.transform.position.y);
        }

        // Show current name in the input field content
        if (nameInput != null)
        {
            nameInput.SetTextWithoutNotify(activeNode.nodeName);
        }

        UpdateStatusText();
    }

    private void OnLoadSfxClicked()
    {
        if (activeNode == null) return;

        FileBrowser.SetFilters(true, new FileBrowser.Filter("Audio Files", ".wav", ".mp3", ".ogg", ".aiff"));
        FileBrowser.SetDefaultFilter(".wav");

        FileBrowser.ShowLoadDialog(
            (paths) =>
            {
                if (paths != null && paths.Length > 0)
                {
                    string path = paths[0];
                    activeNode.LoadAudioFile(path);

                    // Update the displayed path to show only the filename
                    if (sfxPathText != null)
                    {
                        sfxPathText.text = string.IsNullOrEmpty(activeNode.audioPath) ? "No File Loaded" : Path.GetFileName(activeNode.audioPath);
                    }

                    // Refresh the UI text to match the newly stopped node
                    UpdateStatusText();
                }
            },
            () => { /* Load Canceled */ },
            FileBrowser.PickMode.Files,
            false,
            null,
            "Select Audio File",
            "Load"
        );
    }

    private void OnPlayClicked()
    {
        if (activeNode == null) return;
        activeNode.isPlaying = true;
        activeNode.SetIcon(true);
        activeNode.ApplySettings(); // Refreshes FMOD state
        UpdateStatusText();
    }

    private void OnStopClicked()
    {
        if (activeNode == null) return;
        activeNode.isPlaying = false;
        activeNode.SetIcon(false);
        activeNode.ApplySettings(); // Refreshes FMOD state
        UpdateStatusText();
    }

    private void OnRecordClicked()
    {
        if (activeNode == null) return;
        
        // This hooks into your newly added method on the SFXNode
        activeNode.RecordCurrentState();
    }

    private void OnLoopChanged(bool isLooping)
    {
        if (activeNode == null) return;
        activeNode.isLooping = isLooping;
        activeNode.ApplySettings();
    }

    private void OnVolumeChanged(float volume)
    {
        if (activeNode == null) return;
        activeNode.volume = volume;
        activeNode.ApplySettings();
    }

    private void OnColorSliderChanged(float _)
    {
        if (activeNode == null) return;

        float r = redSlider != null ? redSlider.value : 255f;
        float g = greenSlider != null ? greenSlider.value : 255f;
        float b = blueSlider != null ? blueSlider.value : 255f;

        // Convert 0..255 -> 0..1 for Color
        var c = new Color(r / 255f, g / 255f, b / 255f, 1f);
        activeNode.SetIconColor(c);
    }

    private void OnHeightChanged(float height)
    {
        if (activeNode == null) return;

        Vector3 position = activeNode.transform.position;
        position.y = height;
        activeNode.transform.position = position;
    }

    private void OnNameInputEndEdit(string newName)
    {
        if (activeNode == null) return;

        activeNode.SetNodeName(newName);

        // The name is now shown directly in the input field content (no separate display needed)
    }

    private void UpdateStatusText()
    {
        statusText.text = activeNode.isPlaying ? "Status: Playing" : "Status: Stopped";
    }

    public void OnDeleteNodeClicked()
    {
        // 1. Make sure we actually have a node selected
        if (activeNode == null) return;

        // 2. Destroy the physical game object in the scene
        // (This triggers the OnDestroy() method in SFXNode.cs, safely stopping FMOD!)
        Destroy(activeNode.gameObject);

        // 3. Wipe the active reference so the UI stops tracking it
        activeNode = null;

        // 4. Hide or reset your settings panel 
        gameObject.SetActive(false); 
    }
}