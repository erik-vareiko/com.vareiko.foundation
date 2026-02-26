namespace Vareiko.Foundation.Observability
{
    public sealed class DiagnosticsSnapshot
    {
        public bool IsBootCompleted;
        public bool IsBootFailed;
        public string LastBootError = string.Empty;
        public bool IsOnline;
        public bool IsLoading;
        public float LoadingProgress;
        public bool IsBackendConfigured;
        public bool IsBackendAuthenticated;
        public int RemoteConfigValues;
        public float LastUpdatedAt;
    }
}
