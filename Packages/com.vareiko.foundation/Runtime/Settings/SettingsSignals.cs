namespace Vareiko.Foundation.Settings
{
    public readonly struct SettingsLoadedSignal
    {
        public readonly GameSettings Settings;

        public SettingsLoadedSignal(GameSettings settings)
        {
            Settings = settings;
        }
    }

    public readonly struct SettingsChangedSignal
    {
        public readonly GameSettings Settings;

        public SettingsChangedSignal(GameSettings settings)
        {
            Settings = settings;
        }
    }
}
