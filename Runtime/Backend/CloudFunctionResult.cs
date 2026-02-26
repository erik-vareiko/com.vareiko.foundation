using System;

namespace Vareiko.Foundation.Backend
{
    [Serializable]
    public readonly struct CloudFunctionResult
    {
        public readonly bool Success;
        public readonly string ResponseJson;
        public readonly string Error;

        public CloudFunctionResult(bool success, string responseJson, string error)
        {
            Success = success;
            ResponseJson = responseJson;
            Error = error;
        }
    }
}
