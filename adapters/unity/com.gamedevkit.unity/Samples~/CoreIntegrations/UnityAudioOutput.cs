using UnityEngine;

public sealed class UnityAudioOutput
{
    private readonly AudioSource _source;

    public UnityAudioOutput(AudioSource source)
    {
        _source = source;
        _source.playOnAwake = false;
        _source.loop = false;
    }

    public void Play(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        _source.Stop();
        _source.clip = clip;
        _source.volume = Mathf.Clamp01(volume);
        _source.Play();
    }

    public void SetVolume(float volume)
    {
        _source.volume = Mathf.Clamp01(volume);
    }

    public void Stop()
    {
        _source.Stop();
        _source.clip = null;
    }
}
