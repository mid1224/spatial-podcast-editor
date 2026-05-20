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

    private FMOD.Sound coreSound;
    private FMOD.Studio.EventInstance steamAudioEvent;
    private FMOD.Studio.EVENT_CALLBACK eventCallback;

    public GameObject playingIcon;
    public GameObject stoppedIcon;

    // Optional text label under the icon (assign a TextMeshPro text in the inspector)
    public TMP_Text nameLabelUnderIcon;

    public void LoadAudioFile(string absolutePath)
    {
        // Save the absolute path so UI can display it
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

        // --- FIX 1: Apply loop state BEFORE the engine starts reading the file ---
        coreSound.setMode(isLooping ? FMOD.MODE.LOOP_NORMAL : FMOD.MODE.LOOP_OFF);

        steamAudioEvent = RuntimeManager.CreateInstance("event:/SteamAudio_SFX");
        steamAudioEvent.setUserData(coreSound.handle);

        eventCallback = new FMOD.Studio.EVENT_CALLBACK(ProgrammerSoundCallback);
        steamAudioEvent.setCallback(eventCallback, FMOD.Studio.EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND);

        steamAudioEvent.start();

        isAudioLoaded = true;
        Update3DPosition();
        ApplySettings();

        // Ensure icons use the currently stored color and name
        ApplyIconColor(iconColor);
        UpdateNameLabel();
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

        // Ensure future replays catch the correct mode
        if (coreSound.hasHandle())
        {
            coreSound.setMode(isLooping ? FMOD.MODE.LOOP_NORMAL : FMOD.MODE.LOOP_OFF);
        }

        // --- FIX 2: Force the live, currently playing channel to update immediately ---
        if (steamAudioEvent.isValid())
        {
            // Dig into the Event wrapper to find the raw channel playing the file
            steamAudioEvent.getChannelGroup(out FMOD.ChannelGroup eventGroup);
            if (eventGroup.hasHandle())
            {
                eventGroup.getNumChannels(out int numChannels);
                for (int i = 0; i < numChannels; i++)
                {
                    eventGroup.getChannel(i, out FMOD.Channel channel);

                    // Override its mode and explicit loop count dynamically!
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
    }

    private void Update3DPosition()
    {
        steamAudioEvent.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
    }

    // Toggle icon visibility; color is applied separately so icons always share the same color
    public void SetIcon(bool isPlaying)
    {
        if (playingIcon != null && stoppedIcon != null)
        {
            playingIcon.SetActive(isPlaying);
            stoppedIcon.SetActive(!isPlaying);

            // Make sure color and name are applied when toggling
            ApplyIconColor(iconColor);
            UpdateNameLabel();
        }
    }

    // Public API to change the shared icon color (call from UI sliders)
    public void SetIconColor(Color color)
    {
        iconColor = color;
        ApplyIconColor(iconColor);
    }

    // Public API to change node name (call from UI)
    public void SetNodeName(string newName)
    {
        nodeName = string.IsNullOrEmpty(newName) ? "New SFX" : newName;
        UpdateNameLabel();
    }

    // Apply the color to both icons, trying common component types and their children
    private void ApplyIconColor(Color color)
    {
        ApplyColorToGameObject(playingIcon, color);
        ApplyColorToGameObject(stoppedIcon, color);
    }

    private void ApplyColorToGameObject(GameObject go, Color color)
    {
        if (go == null) return;

        // UI Graphic components (Image, Text, etc.)
        var graphics = go.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        foreach (var g in graphics)
        {
            g.color = color;
        }

        // TextMeshPro components
        var tmpros = go.GetComponentsInChildren<TMPro.TMP_Text>(true);
        foreach (var t in tmpros)
        {
            t.color = color;
        }

        // SpriteRenderers
        var sprs = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var s in sprs)
        {
            s.color = color;
        }

        // Mesh / other renderers - try to set _Color if available (this will create instances of materials if necessary)
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            // Avoid changing UI CanvasRenderer (handled by Graphic) though it won't usually expose material color
            try
            {
                if (r.material != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = color;
                }
            }
            catch (UnityException)
            {
                // Some renderers/materials may throw if they use sharedMaterial or are not modifiable at runtime.
                // Ignore those.
            }
        }
    }

    private void UpdateNameLabel()
    {
        if (nameLabelUnderIcon != null)
        {
            nameLabelUnderIcon.text = nodeName;
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