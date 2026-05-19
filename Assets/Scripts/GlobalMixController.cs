using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GlobalMixController : MonoBehaviour
{
    [Header("Scene References")]
    public VocalNode vocalNode;

    [Header("Vocal & Timeline UI")]
    public TMP_InputField filePathInput;
    public Button loadAudioButton;
    public Button playPauseButton;
    public Slider timelineSlider;
    public Slider vocalVolumeSlider;

    [Header("Global Mix UI")]
    public Slider masterVolumeSlider;
    public Slider duckingSlider;
    public TMP_Dropdown reverbDropdown;
    public Slider reverbIntensitySlider;

    private bool isPlaying = false;
    private bool isScrubbing = false; // Prevents the slider from fighting the user when dragged

    void Start()
    {
        // 1. Link UI Events
        loadAudioButton.onClick.AddListener(OnLoadAudioClicked);
        playPauseButton.onClick.AddListener(TogglePlayPause);

        // Timeline dragging events
        timelineSlider.onValueChanged.AddListener(OnTimelineScrubbed);

        // Vocal Volume
        vocalVolumeSlider.onValueChanged.AddListener(v => vocalNode.SetVolume(v));

        // Global Mix Events (We will hook these to FMOD/Steam Audio later)
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        duckingSlider.onValueChanged.AddListener(OnDuckingChanged);
        reverbDropdown.onValueChanged.AddListener(OnReverbPresetChanged);
        reverbIntensitySlider.onValueChanged.AddListener(OnReverbIntensityChanged);
        vocalVolumeSlider.onValueChanged.AddListener(OnVocalVolumeChanged);
    }

    void Update()
    {
        // 2. Automatically move the Timeline Slider as the audio plays
        if (isPlaying && vocalNode.isAudioLoaded && !isScrubbing)
        {
            vocalNode.vocalChannel.getPosition(out uint currentPositionMs, FMOD.TIMEUNIT.MS);

            // Update slider value without triggering the onValueChanged event
            timelineSlider.SetValueWithoutNotify(currentPositionMs);

            // Auto-stop at the end
            if (currentPositionMs >= vocalNode.totalLengthMs - 50)
            {
                TogglePlayPause();
                timelineSlider.SetValueWithoutNotify(0);
                vocalNode.vocalChannel.setPosition(0, FMOD.TIMEUNIT.MS);
            }
        }
    }

    private void OnLoadAudioClicked()
    {
        string path = filePathInput.text;
        // Strip quotes if user copied path directly from Windows
        path = path.Replace("\"", "");

        vocalNode.LoadAudioFile(path);

        // Set up the timeline slider matching the track length
        timelineSlider.maxValue = vocalNode.totalLengthMs;
        timelineSlider.value = 0;
    }

    private void TogglePlayPause()
    {
        if (!vocalNode.isAudioLoaded) return;

        isPlaying = !isPlaying;
        vocalNode.vocalChannel.setPaused(!isPlaying);
    }

    // Called when the user clicks and drags the timeline slider
    private void OnTimelineScrubbed(float newPositionMs)
    {
        if (!vocalNode.isAudioLoaded) return;

        // Tell FMOD to jump to the new timestamp
        vocalNode.vocalChannel.setPosition((uint)newPositionMs, FMOD.TIMEUNIT.MS);
    }

    // --- Global Mix FMOD Hooks ---
    private void OnMasterVolumeChanged(float vol)
    {
        // Sets the FMOD Global Parameter named "MasterVolume"
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("MasterVolume", vol);
    }

    private void OnDuckingChanged(float duckAmount)
    {
        // Controls the sidechain compression intensity
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("DuckingAmount", duckAmount);
    }

    private void OnReverbPresetChanged(int index)
    {
        // Sends 0, 1, 2, 3, or 4 to FMOD to switch the Steam Audio Preset
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("ReverbPreset", index);
    }

    private void OnReverbIntensityChanged(float intensity)
    {
        // Controls the wetness of the Steam Audio Reverb
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("ReverbIntensity", intensity);
    }

    private void OnVocalVolumeChanged(float vol)
    {
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("VocalVolume", vol);
    }
}