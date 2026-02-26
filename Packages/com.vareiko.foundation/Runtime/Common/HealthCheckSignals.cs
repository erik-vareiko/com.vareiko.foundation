namespace Vareiko.Foundation.Common
{
    public readonly struct HealthCheckPassedSignal
    {
        public readonly string CheckName;
        public readonly string Message;

        public HealthCheckPassedSignal(string checkName, string message)
        {
            CheckName = checkName;
            Message = message;
        }
    }

    public readonly struct HealthCheckFailedSignal
    {
        public readonly string CheckName;
        public readonly string Message;

        public HealthCheckFailedSignal(string checkName, string message)
        {
            CheckName = checkName;
            Message = message;
        }
    }
}
