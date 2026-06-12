using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Backend;

namespace Vareiko.Foundation.Save
{
    public enum CloudSaveSyncAction
    {
        None = 0,
        PushedLocalToCloud = 1,
        PulledCloudToLocal = 2,
        ResolvedKeepLocal = 3,
        ResolvedUseCloud = 4,
        ResolvedMerge = 5
    }

    public readonly struct CloudSaveSyncResult
    {
        public readonly bool Success;
        public readonly CloudSaveSyncAction Action;
        public readonly SaveConflictChoice ConflictChoice;
        public readonly BackendErrorCode BackendErrorCode;
        public readonly string Error;

        public CloudSaveSyncResult(
            bool success,
            CloudSaveSyncAction action,
            SaveConflictChoice conflictChoice,
            BackendErrorCode backendErrorCode,
            string error)
        {
            Success = success;
            Action = action;
            ConflictChoice = conflictChoice;
            BackendErrorCode = backendErrorCode;
            Error = error ?? string.Empty;
        }

        public static CloudSaveSyncResult Succeed(CloudSaveSyncAction action, SaveConflictChoice conflictChoice = SaveConflictChoice.KeepLocal)
        {
            return new CloudSaveSyncResult(true, action, conflictChoice, BackendErrorCode.None, string.Empty);
        }

        public static CloudSaveSyncResult Fail(string error, BackendErrorCode backendErrorCode = BackendErrorCode.Unknown)
        {
            return new CloudSaveSyncResult(false, CloudSaveSyncAction.None, SaveConflictChoice.KeepLocal, backendErrorCode, error ?? string.Empty);
        }
    }

    public interface ICloudSaveSyncService
    {
        UniTask<CloudSaveSyncResult> PushAsync<T>(string slot, string key, T fallback = default, CancellationToken cancellationToken = default);
        UniTask<CloudSaveSyncResult> PullAsync<T>(string slot, string key, T fallback = default, CancellationToken cancellationToken = default);
        UniTask<CloudSaveSyncResult> SyncAsync<T>(string slot, string key, T fallback = default, CancellationToken cancellationToken = default);
    }
}
