using UnityEngine;
using FMODUnity;

public class VocalNode : MonoBehaviour
{
    // Defaulting to false so new nodes spawn silently
    public bool isPlaying = false;
    public float volume = 1.0f;
    public bool isAudioLoaded = false;

    // Kept public so the Timeline slider can still read them!
    public uint totalLengthMs;
    public FMOD.Channel vocalChannel;

    private FMOD.Sound vocalSound;

    public void LoadAudioFile(string absolutePath)
    {
        // --- 1. CLEAN UP ORPHANED AUDIO ---
        ReleaseCurrentAudio();

        // --- 2. FORCE STOP STATE ---
        isPlaying = false;

        FMOD.RESULT result = RuntimeManager.CoreSystem.createSound(
            absolutePath,
            FMOD.MODE._3D | FMOD.MODE.CREATESTREAM,
            out vocalSound
        );

        if (result == FMOD.RESULT.OK)
        {
            vocalSound.getLength(out totalLengthMs, FMOD.TIMEUNIT.MS);

            // Start paused so it doesn't overlap or play immediately
            RuntimeManager.CoreSystem.playSound(vocalSound, new FMOD.ChannelGroup(), true, out vocalChannel);

            // Force routing sync into the Vocal Bus for ducking
            RuntimeManager.StudioSystem.flushCommands();
            FMOD.Studio.Bus vocalBus = RuntimeManager.GetBus("bus:/Vocal");

            if (vocalBus.getChannelGroup(out FMOD.ChannelGroup vocalGroup) == FMOD.RESULT.OK)
            {
                vocalChannel.setChannelGroup(vocalGroup);
            }

            // Standard 3D Spatialization
            vocalChannel.set3DMinMaxDistance(1f, 50f);
            vocalChannel.set3DLevel(1.0f);
            vocalChannel.set3DSpread(0f); // Prevents stereo files from ignoring panning

            isAudioLoaded = true;
            Update3DPosition();
            ApplySettings();
        }
        else
        {
            Debug.LogError($"[Vocal Node] Failed to load: {result}");
        }
    }

    // --- NEW: DEDICATED CLEANUP METHOD ---
    private void ReleaseCurrentAudio()
    {
        if (isAudioLoaded)
        {
            // Stop and wipe the channel
            if (vocalChannel.hasHandle())
            {
                vocalChannel.stop();
                vocalChannel.clearHandle();
            }

            // Shred the audio file from RAM
            if (vocalSound.hasHandle())
            {
                vocalSound.release();
                vocalSound.clearHandle();
            }

            isAudioLoaded = false;
        }
    }

    public void ApplySettings()
    {
        if (!isAudioLoaded) return;

        vocalChannel.setVolume(volume);
        vocalChannel.setPaused(!isPlaying);
    }

    void Update()
    {
        if (isAudioLoaded)
        {
            // Sync the internal bool with FMOD's actual play state
            vocalChannel.getPaused(out bool isPaused);
            isPlaying = !isPaused;

            Update3DPosition();
        }
    }

    private void Update3DPosition()
    {
        FMOD.VECTOR pos = RuntimeUtils.ToFMODVector(transform.position);
        FMOD.VECTOR vel = RuntimeUtils.ToFMODVector(Vector3.zero);
        vocalChannel.set3DAttributes(ref pos, ref vel);
    }

    void OnDestroy()
    {
        // Safe cleanup if the node is deleted from the scene
        ReleaseCurrentAudio();
    }

    public void SetVolume(float newVolume)
    {
        volume = newVolume;
        if (isAudioLoaded)
        {
            vocalChannel.setVolume(volume);
        }
    }
}