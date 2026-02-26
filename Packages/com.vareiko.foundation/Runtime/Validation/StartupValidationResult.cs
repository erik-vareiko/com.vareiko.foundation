namespace Vareiko.Foundation.Validation
{
    public readonly struct StartupValidationResult
    {
        public readonly bool IsValid;
        public readonly string Message;
        public readonly StartupValidationSeverity Severity;

        public StartupValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
            Severity = isValid ? StartupValidationSeverity.Info : StartupValidationSeverity.Error;
        }

        public StartupValidationResult(StartupValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
            IsValid = severity != StartupValidationSeverity.Error;
        }

        public static StartupValidationResult Success(string message = "")
        {
            return new StartupValidationResult(StartupValidationSeverity.Info, message);
        }

        public static StartupValidationResult Warning(string message)
        {
            return new StartupValidationResult(StartupValidationSeverity.Warning, message);
        }

        public static StartupValidationResult Fail(string message)
        {
            return new StartupValidationResult(StartupValidationSeverity.Error, message);
        }
    }
}
