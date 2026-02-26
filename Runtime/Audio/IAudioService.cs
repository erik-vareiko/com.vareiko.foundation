using UnityEngine;

namespace Vareiko.Foundation.Audio
{
    public interface IAudioService
    {
        void SetMasterVolume(float value);
        void SetMusicVolume(float value);
        void SetSfxVolume(float value);
        void PlayMusic(AudioClip clip, bool loop = true, float volumeScale = 1f);
        void StopMusic();
        void PlaySfx(AudioClip clip, float volumeScale = 1f);
    }
}
