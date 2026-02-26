using UnityEngine;

namespace Vareiko.Foundation.AssetManagement
{
    public readonly struct AssetLoadResult<T> where T : Object
    {
        public readonly bool Success;
        public readonly T Asset;
        public readonly string Error;

        public AssetLoadResult(bool success, T asset, string error)
        {
            Success = success;
            Asset = asset;
            Error = error;
        }

        public static AssetLoadResult<T> Succeed(T asset)
        {
            return new AssetLoadResult<T>(asset != null, asset, asset != null ? string.Empty : "Asset is null.");
        }

        public static AssetLoadResult<T> Fail(string error)
        {
            return new AssetLoadResult<T>(false, null, error ?? "Unknown asset load error.");
        }
    }
}
