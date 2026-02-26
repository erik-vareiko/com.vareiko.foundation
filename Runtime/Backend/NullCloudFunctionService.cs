using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Backend
{
    public sealed class NullCloudFunctionService : ICloudFunctionService
    {
        public UniTask<CloudFunctionResult> ExecuteAsync(string functionName, string payloadJson = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(new CloudFunctionResult(false, string.Empty, "Cloud functions are not configured."));
        }
    }
}
