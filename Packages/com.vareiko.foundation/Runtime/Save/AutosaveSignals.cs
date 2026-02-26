namespace Vareiko.Foundation.Save
{
    public readonly struct AutosaveTriggeredSignal
    {
        public readonly string Reason;
        public readonly bool SaveSettings;
        public readonly bool SaveConsent;

        public AutosaveTriggeredSignal(string reason, bool saveSettings, bool saveConsent)
        {
            Reason = reason ?? string.Empty;
            SaveSettings = saveSettings;
            SaveConsent = saveConsent;
        }
    }

    public readonly struct AutosaveCompletedSignal
    {
        public readonly string Reason;
        public readonly int SavedTargets;

        public AutosaveCompletedSignal(string reason, int savedTargets)
        {
            Reason = reason ?? string.Empty;
            SavedTargets = savedTargets;
        }
    }

    public readonly struct AutosaveFailedSignal
    {
        public readonly string Reason;
        public readonly string Error;

        public AutosaveFailedSignal(string reason, string error)
        {
            Reason = reason ?? string.Empty;
            Error = error ?? string.Empty;
        }
    }
}
