using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using TMPro;
using System;
using System.Collections.Generic;

[Serializable]
public class VocalNodeState
{
    public float timeMs;
    public Vector3 position;
    public float volume;
    public float minDistance;
    public float maxDistance;
}

public class VocalNode : MonoBehaviour
{
    public bool isPlaying = false;
    public float volume = 1.0f;
    public bool isAudioLoaded = false;

    // --- Spatial Settings ---
    public float minDistance = 1f;
    public float maxDistance = 50f;

    [Header("Spatial UI Controls")]
    public Slider minDistanceSlider;
    public Slider maxDistanceSlider;

    public uint totalLengthMs;
    public FMOD.Channel vocalChannel;
    private FMOD.Sound vocalSound;

    // Selection underline support
    public bool isSelected = false;
    public TMP_Text nameLabelUnderIcon;

    // Optional text label for displaying distance
    public TMP_Text distanceLabelUnderName;

    [Header("Timeline Recording")]
    public List<VocalNodeState> recordedStates = new List<VocalNodeState>();
    public bool enableTimelinePlayback = true;
    private GlobalMixController globalMixController;

    // ADDED: Track the last time we updated to prevent timeline-locking while dragging
    private float lastEvaluatedTime = -1f;

    void Start()
    {
        // Safely Initialize UI Sliders
        if (minDistanceSlider != null)
        {
            minDistanceSlider.minValue = 0f;
            minDistanceSlider.maxValue = 50f;
            minDistanceSlider.SetValueWithoutNotify(minDistance);
            minDistanceSlider.onValueChanged.AddListener(SetMinDistance);
        }

        if (maxDistanceSlider != null)
        {
            maxDistanceSlider.minValue = 1f;
            maxDistanceSlider.maxValue = 100f;
            maxDistanceSlider.SetValueWithoutNotify(maxDistance);
            maxDistanceSlider.onValueChanged.AddListener(SetMaxDistance);
        }
    }

    public void LoadAudioFile(string absolutePath)
    {
        ReleaseCurrentAudio();
        isPlaying = false;

        FMOD.RESULT result = RuntimeManager.CoreSystem.createSound(absolutePath, FMOD.MODE._3D | FMOD.MODE.CREATESTREAM, out vocalSound);

        if (result == FMOD.RESULT.OK)
        {
            vocalSound.getLength(out totalLengthMs, FMOD.TIMEUNIT.MS);
            RuntimeManager.CoreSystem.playSound(vocalSound, new FMOD.ChannelGroup(), true, out vocalChannel);

            // Route to the Vocal Bus for Ducking
            RuntimeManager.StudioSystem.flushCommands();
            FMOD.Studio.Bus vocalBus = RuntimeManager.GetBus("bus:/Vocal");
            if (vocalBus.getChannelGroup(out FMOD.ChannelGroup vocalGroup) == FMOD.RESULT.OK)
            {
                vocalChannel.setChannelGroup(vocalGroup);
            }

            // Standard FMOD 3D Panning
            vocalChannel.set3DLevel(1.0f);

            isAudioLoaded = true;

            ApplySpatialSettings();
            Update3DPosition();
            ApplySettings();
            UpdateDistanceLabel();
        }
    }

    private void ReleaseCurrentAudio()
    {
        if (isAudioLoaded)
        {
            if (vocalChannel.hasHandle()) { vocalChannel.stop(); vocalChannel.clearHandle(); }
            if (vocalSound.hasHandle()) { vocalSound.release(); vocalSound.clearHandle(); }
            isAudioLoaded = false;
        }
    }

    public void ApplySettings()
    {
        if (!isAudioLoaded) return;
        vocalChannel.setVolume(volume);
        vocalChannel.setPaused(!isPlaying);
    }

    // --- Spatial Control Handlers ---
    private void ApplySpatialSettings()
    {
        if (isAudioLoaded && vocalChannel.hasHandle())
        {
            vocalChannel.set3DMinMaxDistance(minDistance, maxDistance);
        }
    }

    public void SetVolume(float newVol)
    {
        volume = newVol;
        ApplySettings();
    }

    public void SetMinDistance(float newMin)
    {
        minDistance = newMin;
        ApplySpatialSettings();
    }

    public void SetMaxDistance(float newMax)
    {
        maxDistance = newMax;
        ApplySpatialSettings();
    }

    // --- NEW: Scrubbing and Replay Controls ---
    public void ScrubTo(uint targetTimeMs)
    {
        if (isAudioLoaded && vocalChannel.hasHandle())
        {
            vocalChannel.setPosition(targetTimeMs, FMOD.TIMEUNIT.MS);
        }
    }

    public void RestartAudio()
    {
        if (isAudioLoaded && vocalChannel.hasHandle())
        {
            vocalChannel.setPosition(0, FMOD.TIMEUNIT.MS);
            ApplySettings();
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (nameLabelUnderIcon != null)
        {
            nameLabelUnderIcon.fontStyle = isSelected ? FontStyles.Underline : FontStyles.Normal;
        }
    }

    // --- Timeline Recording Methods --- //
    public void RecordCurrentState()
    {
        float currentTime = 0f;

        if (globalMixController == null)
            globalMixController = FindObjectOfType<GlobalMixController>();

        if (globalMixController != null && globalMixController.timelineSlider != null)
        {
            currentTime = globalMixController.timelineSlider.value;
        }

        RecordState(currentTime);
    }

    public void RecordState(float timeMs)
    {
        VocalNodeState newState = new VocalNodeState
        {
            timeMs = timeMs,
            position = transform.position,
            volume = volume,
            minDistance = minDistance,
            maxDistance = maxDistance
        };

        // If a keyframe already exists around this time, replace it
        int existingIndex = recordedStates.FindIndex(s => Mathf.Approximately(s.timeMs, timeMs));
        if (existingIndex >= 0)
        {
            recordedStates[existingIndex] = newState;
        }
        else
        {
            recordedStates.Add(newState);
            recordedStates.Sort((a, b) => a.timeMs.CompareTo(b.timeMs));
        }

        // ADDED: Force the cached time to update so it knows we overwrote the state
        lastEvaluatedTime = timeMs;

        Debug.Log($"[Vocal Node] Recorded settings & position at timeline {timeMs}ms");
    }

    public void EvaluateTimeline(float currentTime)
    {
        if (recordedStates.Count == 0) return;

        // ONLY evaluate if the timeline has actually moved.
        // This allows the user to freely drag the node around while the timeline is paused!
        if (Mathf.Approximately(currentTime, lastEvaluatedTime)) return;
        lastEvaluatedTime = currentTime;

        // Snap to first state if playback is before the first recorded state
        if (currentTime <= recordedStates[0].timeMs)
        {
            ApplyStateSnap(recordedStates[0]);
            return;
        }

        // Snap to last state if playback exceeded final record
        if (currentTime >= recordedStates[recordedStates.Count - 1].timeMs)
        {
            ApplyStateSnap(recordedStates[recordedStates.Count - 1]);
            return;
        }

        // Find current timeline segment
        for (int i = 0; i < recordedStates.Count - 1; i++)
        {
            VocalNodeState current = recordedStates[i];
            VocalNodeState next = recordedStates[i + 1];

            if (currentTime >= current.timeMs && currentTime <= next.timeMs)
            {
                // Smoothly interpolate position
                float t = (currentTime - current.timeMs) / (next.timeMs - current.timeMs);
                transform.position = Vector3.Lerp(current.position, next.position, t);

                // Snap configuration settings to current timeframe key
                SnapSettings(current);
                return;
            }
        }
    }

    private void ApplyStateSnap(VocalNodeState state)
    {
        transform.position = state.position;
        SnapSettings(state);
    }

    private void SnapSettings(VocalNodeState state)
    {
        bool changedSettings = false;
        bool changedSpatial = false;

        if (!Mathf.Approximately(volume, state.volume))
        {
            volume = state.volume;
            changedSettings = true;
        }

        if (!Mathf.Approximately(minDistance, state.minDistance) ||
            !Mathf.Approximately(maxDistance, state.maxDistance))
        {
            minDistance = state.minDistance;
            maxDistance = state.maxDistance;
            changedSpatial = true;

            // Sync UI Sliders if available, without triggering recursive updates
            if (minDistanceSlider != null) minDistanceSlider.SetValueWithoutNotify(minDistance);
            if (maxDistanceSlider != null) maxDistanceSlider.SetValueWithoutNotify(maxDistance);
        }

        if (changedSettings)
        {
            ApplySettings();
        }

        if (changedSpatial)
        {
            ApplySpatialSettings();
        }
    }

    void Update()
    {
        if (enableTimelinePlayback)
        {
            // Only evaluate the timeline if we are actively playing or scrubbing
            if (isPlaying || (globalMixController != null && globalMixController.isScrubbing))
            {
                if (globalMixController == null)
                {
                    globalMixController = FindFirstObjectByType<GlobalMixController>();
                }
    
                if (globalMixController != null && globalMixController.timelineSlider != null)
                {
                    EvaluateTimeline(globalMixController.timelineSlider.value);
                }
            }
        }

        if (isAudioLoaded)
        {
            vocalChannel.getPaused(out bool isPaused);
            isPlaying = !isPaused;
            Update3DPosition();
        }

        UpdateDistanceLabel();
    }

    private void Update3DPosition()
    {
        FMOD.VECTOR pos = RuntimeUtils.ToFMODVector(transform.position);
        FMOD.VECTOR vel = RuntimeUtils.ToFMODVector(Vector3.zero);
        vocalChannel.set3DAttributes(ref pos, ref vel);
    }

    private void UpdateDistanceLabel()
    {
        if (distanceLabelUnderName != null)
        {
            float distance = Vector3.Distance(transform.position, Vector3.zero);
            float height = transform.position.y;
            
            string label = $"{distance:F2}m";
            if (!Mathf.Approximately(height, 0f))
            {
                label += $" [h: {height:F2}m]";
            }
            
            distanceLabelUnderName.text = label;
        }
    }

    void OnDestroy()
    {
        if (minDistanceSlider != null) minDistanceSlider.onValueChanged.RemoveListener(SetMinDistance);
        if (maxDistanceSlider != null) maxDistanceSlider.onValueChanged.RemoveListener(SetMaxDistance);
        
        ReleaseCurrentAudio();
    }
}