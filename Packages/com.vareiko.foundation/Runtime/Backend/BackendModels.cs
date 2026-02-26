using System;
using System.Collections.Generic;

namespace Vareiko.Foundation.Backend
{
    [Serializable]
    public readonly struct BackendAuthResult
    {
        public readonly bool Success;
        public readonly string PlayerId;
        public readonly string Error;

        public BackendAuthResult(bool success, string playerId, string error)
        {
            Success = success;
            PlayerId = playerId;
            Error = error;
        }
    }

    [Serializable]
    public readonly struct BackendPlayerDataResult
    {
        public readonly bool Success;
        public readonly IReadOnlyDictionary<string, string> Data;
        public readonly string Error;

        public BackendPlayerDataResult(bool success, IReadOnlyDictionary<string, string> data, string error)
        {
            Success = success;
            Data = data;
            Error = error;
        }
    }
}
