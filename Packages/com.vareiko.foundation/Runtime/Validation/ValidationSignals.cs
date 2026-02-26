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

    public readonly struct StartupValidationWarningSignal
    {
        public readonly string RuleName;
        public readonly string Message;

        public StartupValidationWarningSignal(string ruleName, string message)
        {
            RuleName = ruleName;
            Message = message;
        }
    }

    public readonly struct StartupValidationCompletedSignal
    {
        public readonly int TotalRules;
        public readonly int PassedCount;
        public readonly int WarningCount;
        public readonly int ErrorCount;

        public bool HasBlockingFailures => ErrorCount > 0;

        public StartupValidationCompletedSignal(int totalRules, int passedCount, int warningCount, int errorCount)
        {
            TotalRules = totalRules;
            PassedCount = passedCount;
            WarningCount = warningCount;
            ErrorCount = errorCount;
        }
    }
}
