using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Backend
{
    public sealed class PlayFabCloudFunctionService : ICloudFunctionService
    {
        private readonly BackendConfig _config;

        [Inject]
        public PlayFabCloudFunctionService([InjectOptional] BackendConfig config = null)
        {
            _config = config;
        }

        public async UniTask<CloudFunctionResult> ExecuteAsync(string functionName, string payloadJson = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(functionName))
            {
                return new CloudFunctionResult(false, string.Empty, "Cloud function name is null or empty.");
            }

            if (_config == null || !_config.EnableCloudFunctions)
            {
                return new CloudFunctionResult(false, string.Empty, "Cloud functions are disabled in backend config.");
            }

            if (_config.Provider != BackendProviderType.PlayFab || string.IsNullOrWhiteSpace(_config.TitleId))
            {
                return new CloudFunctionResult(false, string.Empty, "PlayFab is not configured.");
            }

#if PLAYFAB_SDK
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            return new CloudFunctionResult(true, "{}", string.Empty);
#else
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            Debug.LogWarning($"PlayFabCloudFunctionService: PLAYFAB_SDK is not defined. Function={functionName.Trim()}");
            return new CloudFunctionResult(false, string.Empty, "PLAYFAB_SDK is not installed.");
#endif
        }
    }
}
