namespace Vareiko.Foundation.Environment
{
    public readonly struct EnvironmentProfileChangedSignal
    {
        public readonly string ProfileId;

        public EnvironmentProfileChangedSignal(string profileId)
        {
            ProfileId = profileId ?? string.Empty;
        }
    }

    public readonly struct EnvironmentValueMissingSignal
    {
        public readonly string ProfileId;
        public readonly string Key;

        public EnvironmentValueMissingSignal(string profileId, string key)
        {
            ProfileId = profileId ?? string.Empty;
            Key = key ?? string.Empty;
        }
    }
}
