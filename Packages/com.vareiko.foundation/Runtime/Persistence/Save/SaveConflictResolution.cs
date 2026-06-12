namespace Vareiko.Foundation.Save
{
    public readonly struct SaveConflictResolution
    {
        public readonly SaveConflictChoice Choice;
        public readonly string Payload;
        public readonly string Error;

        public SaveConflictResolution(SaveConflictChoice choice, string payload, string error)
        {
            Choice = choice;
            Payload = payload ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public static SaveConflictResolution Local(string payload)
        {
            return new SaveConflictResolution(SaveConflictChoice.KeepLocal, payload, string.Empty);
        }

        public static SaveConflictResolution Cloud(string payload)
        {
            return new SaveConflictResolution(SaveConflictChoice.UseCloud, payload, string.Empty);
        }
    }
}
