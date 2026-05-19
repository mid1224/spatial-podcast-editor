using UnityEngine;
using FMODUnity;

public class SFXNode : MonoBehaviour
{
    public bool isPlaying = false;
    public bool isLooping = false;
    public float volume = 1.0f;

    private FMOD.Sound sfxSound;
    private FMOD.Channel sfxChannel;
    private bool isAudioLoaded = false;

    // Call this exactly like we do for the Vocal Node
    public void LoadAudioFile(string absolutePath)
    {
        // 1. Load the raw sound file
        FMOD.RESULT loadResult = RuntimeManager.CoreSystem.createSound(
            absolutePath,
            FMOD.MODE._3D | FMOD.MODE.CREATESTREAM,
            out sfxSound
        );

        if (loadResult != FMOD.RESULT.OK)
        {
            Debug.LogError($"[SFX Node] Load failed: {loadResult}");
            return;
        }

        // 2. Play the sound on a raw independent core channel (initially paused)
        RuntimeManager.CoreSystem.playSound(sfxSound, new FMOD.ChannelGroup(), true, out sfxChannel);

        // --- THE BULLETPROOF ROUTING FIX ---
        // Force FMOD Studio to finish creating its mixer tracks right now
        RuntimeManager.StudioSystem.flushCommands();

        FMOD.Studio.Bus sfxBus = RuntimeManager.GetBus("bus:/SFX");
        FMOD.RESULT groupResult = sfxBus.getChannelGroup(out FMOD.ChannelGroup sfxGroup);

        if (groupResult == FMOD.RESULT.OK)
        {
            // Explicitly set the group. This forces the sound into your SFX mixer bus!
            sfxChannel.setChannelGroup(sfxGroup);
            Debug.Log("[SFX Node] Successfully routed through the SFX Bus!");
        }
        else
        {
            Debug.LogError($"[SFX Node] Bus routing failed: {groupResult}. Audio will bypass Ducking.");
        }
        // ------------------------------------

        // 3. Configure 3D Point Source Settings
        sfxChannel.set3DMinMaxDistance(1f, 50f);
        sfxChannel.set3DLevel(1.0f); // Forces 100% spatial execution

        isAudioLoaded = true;
        Update3DPosition();
        ApplySettings();
    }

    public void ApplySettings()
    {
        if (!isAudioLoaded) return;

        sfxChannel.setVolume(volume);
        sfxSound.setMode(isLooping ? FMOD.MODE.LOOP_NORMAL : FMOD.MODE.LOOP_OFF);
        sfxChannel.setPaused(!isPlaying);
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
        FMOD.VECTOR pos = RuntimeUtils.ToFMODVector(transform.position);
        FMOD.VECTOR vel = RuntimeUtils.ToFMODVector(Vector3.zero);
        sfxChannel.set3DAttributes(ref pos, ref vel);
    }
}