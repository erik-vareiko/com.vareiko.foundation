namespace Vareiko.Foundation.Audio
{
    public readonly struct AudioVolumesChangedSignal
    {
        public readonly float Master;
        public readonly float Music;
        public readonly float Sfx;

        public AudioVolumesChangedSignal(float master, float music, float sfx)
        {
            Master = master;
            Music = music;
            Sfx = sfx;
        }
    }
}
