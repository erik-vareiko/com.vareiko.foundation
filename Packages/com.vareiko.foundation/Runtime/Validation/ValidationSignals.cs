namespace Vareiko.Foundation.Validation
{
    public readonly struct StartupValidationPassedSignal
    {
        public readonly string RuleName;
        public readonly string Message;

        public StartupValidationPassedSignal(string ruleName, string message)
        {
            RuleName = ruleName;
            Message = message;
        }
    }

    public readonly struct StartupValidationFailedSignal
    {
        public readonly string RuleName;
        public readonly string Message;

        public StartupValidationFailedSignal(string ruleName, string message)
        {
            RuleName = ruleName;
            Message = message;
        }
    }
}
