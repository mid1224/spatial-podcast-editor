using UnityEngine;
using FMODUnity;

public class VocalNode : MonoBehaviour
{
    public bool isPlaying = false;
    public float volume = 1.0f;
    public bool isAudioLoaded = false;

    public uint totalLengthMs;
    public FMOD.Channel vocalChannel;
    private FMOD.Sound vocalSound;

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
            vocalChannel.set3DMinMaxDistance(1f, 50f);
            vocalChannel.set3DLevel(1.0f);
            vocalChannel.set3DSpread(0f);

            isAudioLoaded = true;
            Update3DPosition();
            ApplySettings();
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

    public void SetVolume(float newVol)
    {
        volume = newVol;
        ApplySettings();
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

    void Update()
    {
        if (isAudioLoaded)
        {
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

    void OnDestroy() { ReleaseCurrentAudio(); }
}