using System;

namespace Vareiko.Foundation.Economy
{
    [Serializable]
    public readonly struct EconomyOperationResult
    {
        public readonly bool Success;
        public readonly string Error;

        public EconomyOperationResult(bool success, string error)
        {
            Success = success;
            Error = error;
        }

        public static EconomyOperationResult Ok()
        {
            return new EconomyOperationResult(true, string.Empty);
        }

        public static EconomyOperationResult Fail(string error)
        {
            return new EconomyOperationResult(false, error ?? "Unknown economy error.");
        }
    }
}
