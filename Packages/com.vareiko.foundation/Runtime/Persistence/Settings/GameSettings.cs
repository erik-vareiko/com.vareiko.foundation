using System;

namespace Vareiko.Foundation.Settings
{
    [Serializable]
    public sealed class GameSettings
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 1f;
        public float SfxVolume = 1f;
        public string Language = "en";
        public bool VibrationEnabled = true;
        public bool InvertY = false;
    }
}
