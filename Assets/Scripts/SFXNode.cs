using UnityEngine;
using FMODUnity;
using System;
using System.Runtime.InteropServices;
using TMPro;

public class SFXNode : MonoBehaviour
{
    public bool isPlaying = false;
    public bool isLooping = false;
    public float volume = 1.0f;
    public bool isAudioLoaded = false;

    // Node name (editable via UI)
    public string nodeName = "New SFX";

    // Icon color (shared for playing and stopped icons)
    public Color iconColor = Color.white;

    // Saved absolute path of the loaded audio file
    public string audioPath = string.Empty;

    // Selection state (controls label style)
    public bool isSelected = false;

    private FMOD.Sound coreSound;
    private FMOD.Studio.EventInstance steamAudioEvent;
    private FMOD.Studio.EVENT_CALLBACK eventCallback;

    public GameObject playingIcon;
    public GameObject stoppedIcon;

    // Optional text label under the icon (assign a TextMeshPro text in the inspector)
    public TMP_Text nameLabelUnderIcon;

    // Optional text label for displaying distance
    public TMP_Text distanceLabelUnderName;

    public void LoadAudioFile(string absolutePath)
    {
        audioPath = absolutePath ?? string.Empty;

        ReleaseCurrentAudio();
        isPlaying = false;

        FMOD.RESULT result = RuntimeManager.CoreSystem.createSound(
            absolutePath,
            FMOD.MODE._3D | FMOD.MODE.CREATESTREAM,
            out coreSound
        );

        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"[SFX Node] Failed to load: {result}");
            return;
        }

        coreSound.setMode(isLooping ? FMOD.MODE.LOOP_NORMAL : FMOD.MODE.LOOP_OFF);

        steamAudioEvent = RuntimeManager.CreateInstance("event:/SteamAudio_SFX");
        steamAudioEvent.setUserData(coreSound.handle);

        eventCallback = new FMOD.Studio.EVENT_CALLBACK(ProgrammerSoundCallback);
        steamAudioEvent.setCallback(eventCallback, FMOD.Studio.EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND);

        steamAudioEvent.start();

        isAudioLoaded = true;
        Update3DPosition();
        ApplySettings();

        ApplyIconColor(iconColor);
        UpdateNameLabel();
        UpdateDistanceLabel();
    }

    private void ReleaseCurrentAudio()
    {
        if (isAudioLoaded)
        {
            if (steamAudioEvent.isValid())
            {
                steamAudioEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                steamAudioEvent.release();
                steamAudioEvent.clearHandle();
            }
            if (coreSound.hasHandle())
            {
                coreSound.release();
                coreSound.clearHandle();
            }
            isAudioLoaded = false;
        }
    }

    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    static FMOD.RESULT ProgrammerSoundCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        FMOD.Studio.EventInstance instance = new FMOD.Studio.EventInstance(instancePtr);
        instance.getUserData(out IntPtr customSoundHandle);
        if (customSoundHandle == IntPtr.Zero) return FMOD.RESULT.OK;

        if (type == FMOD.Studio.EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND)
        {
            var parameter = (FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.PROGRAMMER_SOUND_PROPERTIES));
            parameter.sound = customSoundHandle;
            parameter.subsoundIndex = -1;
            Marshal.StructureToPtr(parameter, parameterPtr, false);
        }
        return FMOD.RESULT.OK;
    }

    public void ApplySettings()
    {
        if (!isAudioLoaded) return;

        steamAudioEvent.setVolume(volume);
        steamAudioEvent.setPaused(!isPlaying);

        if (coreSound.hasHandle())
        {
            coreSound.setMode(isLooping ? FMOD.MODE.LOOP_NORMAL : FMOD.MODE.LOOP_OFF);
        }

        if (steamAudioEvent.isValid())
        {
            steamAudioEvent.getChannelGroup(out FMOD.ChannelGroup eventGroup);
            if (eventGroup.hasHandle())
            {
                eventGroup.getNumChannels(out int numChannels);
                for (int i = 0; i < numChannels; i++)
                {
                    eventGroup.getChannel(i, out FMOD.Channel channel);
                    channel.setMode(isLooping ? FMOD.MODE.LOOP_NORMAL : FMOD.MODE.LOOP_OFF);
                    channel.setLoopCount(isLooping ? -1 : 0);
                }
            }
        }
    }

    void Update()
    {
        if (isPlaying && isAudioLoaded)
        {
            Update3DPosition();
        }

        UpdateDistanceLabel();
    }

    private void Update3DPosition()
    {
        steamAudioEvent.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
    }

    private void UpdateDistanceLabel()
    {
        if (distanceLabelUnderName != null)
        {
            float distance = Vector3.Distance(transform.position, Vector3.zero);
            distanceLabelUnderName.text = $"{distance:F2}m";
        }
    }

    public void SetIcon(bool isPlaying)
    {
        if (playingIcon != null && stoppedIcon != null)
        {
            playingIcon.SetActive(isPlaying);
            stoppedIcon.SetActive(!isPlaying);

            ApplyIconColor(iconColor);
            UpdateNameLabel();
        }
    }

    public void SetIconColor(Color color)
    {
        iconColor = color;
        ApplyIconColor(iconColor);
    }

    public void SetNodeName(string newName)
    {
        nodeName = string.IsNullOrEmpty(newName) ? "New SFX" : newName;
        UpdateNameLabel();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateNameLabel();
    }

    private void ApplyIconColor(Color color)
    {
        ApplyColorToGameObject(playingIcon, color);
        ApplyColorToGameObject(stoppedIcon, color);
    }

    private void ApplyColorToGameObject(GameObject go, Color color)
    {
        if (go == null) return;

        var graphics = go.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        foreach (var g in graphics) g.color = color;

        var tmpros = go.GetComponentsInChildren<TMPro.TMP_Text>(true);
        foreach (var t in tmpros) t.color = color;

        var sprs = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var s in sprs) s.color = color;

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            try
            {
                if (r.material != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = color;
                }
            }
            catch (UnityException)
            {
            }
        }
    }

    private void UpdateNameLabel()
    {
        if (nameLabelUnderIcon != null)
        {
            nameLabelUnderIcon.text = nodeName;
            nameLabelUnderIcon.fontStyle = isSelected ? FontStyles.Underline : FontStyles.Normal;
        }
    }

    public void SetIconAndColor(bool playing, Color color)
    {
        SetIconColor(color);
        SetIcon(playing);
    }

    void OnDestroy()
    {
        ReleaseCurrentAudio();
    }
}