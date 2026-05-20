using UnityEngine;
using FMODUnity;
using System;
using System.Runtime.InteropServices;

public class SFXNode : MonoBehaviour
{
    public bool isPlaying = false;
    public bool isLooping = false;
    public float volume = 1.0f;
    public bool isAudioLoaded = false;

    private FMOD.Sound coreSound;
    private FMOD.Studio.EventInstance steamAudioEvent;
    private FMOD.Studio.EVENT_CALLBACK eventCallback;

    public void LoadAudioFile(string absolutePath)
    {
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

    void OnDestroy()
    {
        ReleaseCurrentAudio();
    }
}