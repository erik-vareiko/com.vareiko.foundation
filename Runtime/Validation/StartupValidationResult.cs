namespace Vareiko.Foundation.Validation
{
    public readonly struct StartupValidationResult
    {
        public readonly bool IsValid;
        public readonly string Message;

        public StartupValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
        }

        public static StartupValidationResult Success(string message = "")
        {
            return new StartupValidationResult(true, message);
        }

        public static StartupValidationResult Fail(string message)
        {
            return new StartupValidationResult(false, message);
        }
    }
}
