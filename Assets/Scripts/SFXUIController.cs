using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SFXUIController : MonoBehaviour
{
    [Header("UI Controls")]
    public Button playButton;
    public Button stopButton;
    public Toggle loopToggle;
    public Slider volumeSlider;
    public TextMeshProUGUI statusText;

    [Header("Audio Loading")]
    public TMP_InputField sfxPathInput;
    public Button loadSfxButton;

    private SFXNode activeNode;

    void Start()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        stopButton.onClick.AddListener(OnStopClicked);
        loopToggle.onValueChanged.AddListener(OnLoopChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        loadSfxButton.onClick.AddListener(OnLoadSfxClicked);

        gameObject.SetActive(false);
    }

    public void BindToNode(SFXNode node)
    {
        activeNode = node;
        gameObject.SetActive(true);

        sfxPathInput.text = "";

        loopToggle.SetIsOnWithoutNotify(activeNode.isLooping);
        volumeSlider.SetValueWithoutNotify(activeNode.volume);
        UpdateStatusText();
    }

    // --- NEW LOADING METHOD ---
    private void OnLoadSfxClicked()
    {
        if (activeNode == null || string.IsNullOrEmpty(sfxPathInput.text)) return;

        string path = sfxPathInput.text;
        path = path.Replace("\"", ""); // Clean up copied Windows file paths

        // Call the previously unused method directly on the selected node!
        activeNode.LoadAudioFile(path);
    }
    // --------------------------

    private void OnPlayClicked()
    {
        if (activeNode == null) return;
        activeNode.isPlaying = true;
        activeNode.ApplySettings(); // Refreshes FMOD state
        UpdateStatusText();
    }

    private void OnStopClicked()
    {
        if (activeNode == null) return;
        activeNode.isPlaying = false;
        activeNode.ApplySettings(); // Refreshes FMOD state
        UpdateStatusText();
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

    private void UpdateStatusText()
    {
        statusText.text = activeNode.isPlaying ? "Status: Playing" : "Status: Stopped";
    }
}