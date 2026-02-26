namespace Vareiko.Foundation.Save
{
    public readonly struct SaveMigrationResult
    {
        public readonly bool Success;
        public readonly int Version;
        public readonly string Payload;
        public readonly string Error;

        public SaveMigrationResult(bool success, int version, string payload, string error)
        {
            Success = success;
            Version = version;
            Payload = payload;
            Error = error;
        }

        public static SaveMigrationResult Succeed(int version, string payload)
        {
            return new SaveMigrationResult(true, version, payload, string.Empty);
        }

        public static SaveMigrationResult Fail(int version, string error)
        {
            return new SaveMigrationResult(false, version, string.Empty, error);
        }
    }
}
