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
    public Button recordButton; // 1. Added Record Button
    public Slider timelineSlider;
    public Slider vocalVolumeSlider;

    [Header("Height Control")]
    public Slider heightSlider;
    public float heightMin = -10f;
    public float heightMax = 10f;

    [Header("Global Mix UI")]
    public Slider masterVolumeSlider;
    public Slider duckingSlider;
    public TMP_Dropdown reverbDropdown;
    public Slider reverbIntensitySlider;

    // We can explicitly check this out in the VocalNode
    public bool IsScrubbing => isScrubbing;

    private bool isPlaying = false;
    public bool isScrubbing = false; // Prevents the slider from fighting the user when dragged

    void Start()
    {
        // 1. Link UI Events
        loadAudioButton.onClick.AddListener(OnLoadAudioClicked);
        playPauseButton.onClick.AddListener(TogglePlayPause);
        
        // Setup Record Button
        if (recordButton != null)
        {
            recordButton.onClick.AddListener(OnRecordClicked);
        }

        // Timeline dragging events
        timelineSlider.onValueChanged.AddListener(OnSliderScrubbed);

        // Vocal Volume
        vocalVolumeSlider.onValueChanged.AddListener(v => vocalNode.SetVolume(v));

        // Safely Initialize Height Slider
        if (heightSlider != null && vocalNode != null)
        {
            heightSlider.minValue = heightMin;
            heightSlider.maxValue = heightMax;
            
            // Set the value silently first to prevent accidental teleportation
            heightSlider.SetValueWithoutNotify(vocalNode.transform.position.y);
            
            // Only add the listener AFTER everything matches
            heightSlider.onValueChanged.AddListener(OnHeightChanged);
        }

        // Global Mix Events (We will hook these to FMOD/Steam Audio later)
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        duckingSlider.onValueChanged.AddListener(OnDuckingChanged);
        reverbDropdown.onValueChanged.AddListener(OnReverbPresetChanged);
        reverbIntensitySlider.onValueChanged.AddListener(OnReverbIntensityChanged);
        vocalVolumeSlider.onValueChanged.AddListener(OnVocalVolumeChanged);

        // Sync the UI with the initial FMOD parameter states on start
        OnMasterVolumeChanged(masterVolumeSlider.value);
        OnVocalVolumeChanged(vocalVolumeSlider.value);
        OnDuckingChanged(duckingSlider.value);
        OnReverbIntensityChanged(reverbIntensitySlider.value);
        OnReverbPresetChanged(reverbDropdown.value);
    }

    void Update()
    {
        // 2. Automatically move the Timeline Slider as the audio plays
        if (isPlaying && vocalNode.isAudioLoaded && !isScrubbing)
        {
            // Ask the raw channel for the exact millisecond
            vocalNode.vocalChannel.getPosition(out uint currentPositionMs, FMOD.TIMEUNIT.MS);
            timelineSlider.SetValueWithoutNotify(currentPositionMs);

            // Auto-Rewind at the end of the song
            if (currentPositionMs >= vocalNode.totalLengthMs - 50)
            {
                TogglePlayPause(); // Sets to Paused
                timelineSlider.SetValueWithoutNotify(0);
                vocalNode.RestartAudio();
                vocalNode.isPlaying = false;
                vocalNode.ApplySettings();
            }
        }
    }

    private void OnRecordClicked()
    {
        if (vocalNode == null) return;
        
        vocalNode.RecordCurrentState();
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

    public void TogglePlayPause()
    {
        if (!vocalNode.isAudioLoaded) return;

        // 1. Flip the master playing state
        isPlaying = !isPlaying;

        // 2. Tell the Vocal Node to update itself
        vocalNode.isPlaying = isPlaying;
        vocalNode.ApplySettings();
    }

    // 1. Called when your mouse CLICKS DOWN on the slider
    public void OnScrubBegin()
    {
        isScrubbing = true;
    }

    // 2. Called when you DRAG the slider
    public void OnSliderScrubbed(float value)
    {
        if (isScrubbing && vocalNode.isAudioLoaded)
        {
            vocalNode.ScrubTo((uint)value);
        }
    }

    // 3. Called when your mouse LETS GO of the slider
    public void OnScrubEnd()
    {
        isScrubbing = false;
    }

    private void OnHeightChanged(float height)
    {
        if (vocalNode == null) return;
        
        Vector3 newPos = vocalNode.transform.position;
        newPos.y = height;
        vocalNode.transform.position = newPos;
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