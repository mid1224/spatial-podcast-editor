using UnityEngine;
using FMODUnity;
using System;
using System.Runtime.InteropServices;

public class SFXNode : MonoBehaviour
{
    // Defaulting to false so new nodes spawn silently
    public bool isPlaying = false;
    public bool isLooping = false;
    public float volume = 1.0f;
    public bool isAudioLoaded = false;

    private FMOD.Sound coreSound;
    private FMOD.Studio.EventInstance steamAudioEvent;
    private FMOD.Studio.EVENT_CALLBACK eventCallback;

    public void LoadAudioFile(string absolutePath)
    {
        // --- 1. CLEAN UP ORPHANED AUDIO ---
        ReleaseCurrentAudio();

        // --- 2. FORCE STOP STATE ---
        isPlaying = false;

        // 3. Load the raw audio file into FMOD memory
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

        // 4. Spawn the Steam Audio Shell Event
        steamAudioEvent = RuntimeManager.CreateInstance("event:/SteamAudio_SFX");
        steamAudioEvent.setUserData(coreSound.handle);

        // 5. Attach the callback hook
        eventCallback = new FMOD.Studio.EVENT_CALLBACK(ProgrammerSoundCallback);
        steamAudioEvent.setCallback(eventCallback, FMOD.Studio.EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND);

        // 6. Start the engine
        steamAudioEvent.start();

        isAudioLoaded = true;
        Update3DPosition();
        ApplySettings();
    }

    // --- NEW: DEDICATED CLEANUP METHOD ---
    private void ReleaseCurrentAudio()
    {
        if (isAudioLoaded)
        {
            // Stop and release the event instance
            if (steamAudioEvent.isValid())
            {
                steamAudioEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                steamAudioEvent.release();
                steamAudioEvent.clearHandle(); // Wipes the pointer
            }

            // Release the raw audio file from RAM
            if (coreSound.hasHandle())
            {
                coreSound.release();
                coreSound.clearHandle(); // Wipes the pointer
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
        coreSound.setMode(isLooping ? FMOD.MODE.LOOP_NORMAL : FMOD.MODE.LOOP_OFF);
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
        // Route destruction through our safe cleanup method to prevent leaks on node deletion
        ReleaseCurrentAudio();
    }
}