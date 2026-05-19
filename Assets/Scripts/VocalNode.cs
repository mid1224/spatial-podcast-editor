using UnityEngine;
using FMODUnity;

public class VocalNode : MonoBehaviour
{
    private FMOD.Sound vocalSound;
    public FMOD.Channel vocalChannel;

    public uint totalLengthMs;
    public bool isAudioLoaded = false;
    private bool isPlaying = false; // Track play state internally

    public void LoadAudioFile(string absolutePath)
    {
        FMOD.RESULT result = RuntimeManager.CoreSystem.createSound(
            absolutePath,
            FMOD.MODE._3D | FMOD.MODE.CREATESTREAM,
            out vocalSound
        );

        if (result == FMOD.RESULT.OK)
        {
            vocalSound.getLength(out totalLengthMs, FMOD.TIMEUNIT.MS);

            RuntimeManager.CoreSystem.playSound(vocalSound, new FMOD.ChannelGroup(), true, out vocalChannel);

            RuntimeManager.StudioSystem.flushCommands();
            FMOD.Studio.Bus vocalBus = RuntimeManager.GetBus("bus:/Vocal");

            if (vocalBus.getChannelGroup(out FMOD.ChannelGroup vocalGroup) == FMOD.RESULT.OK)
            {
                vocalChannel.setChannelGroup(vocalGroup);
            }

            vocalChannel.set3DMinMaxDistance(1f, 50f);
            vocalChannel.set3DLevel(1.0f);

            isAudioLoaded = true;
            Update3DPosition(); // Set initial position
        }
    }

    // --- NEW TRACKING LOGIC ---
    void Update()
    {
        if (isAudioLoaded)
        {
            // Check if FMOD is actively playing to sync our update loop
            vocalChannel.getPaused(out bool isPaused);
            isPlaying = !isPaused;

            // Constantly update FMOD with the node's current Unity position
            Update3DPosition();
        }
    }

    private void Update3DPosition()
    {
        FMOD.VECTOR pos = RuntimeUtils.ToFMODVector(transform.position);
        FMOD.VECTOR vel = RuntimeUtils.ToFMODVector(Vector3.zero);
        vocalChannel.set3DAttributes(ref pos, ref vel);
    }
    // ---------------------------

    public void SetVolume(float volume)
    {
        if (isAudioLoaded) vocalChannel.setVolume(volume);
    }
}