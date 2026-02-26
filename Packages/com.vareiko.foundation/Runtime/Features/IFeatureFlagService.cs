using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Features
{
    public interface IFeatureFlagService
    {
        bool IsEnabled(string key, bool fallback = false);
        int GetInt(string key, int fallback = 0);
        float GetFloat(string key, float fallback = 0f);
        string GetString(string key, string fallback = "");
        void SetLocalOverride(string key, bool value);
        void ClearLocalOverrides();
        UniTask RefreshAsync(CancellationToken cancellationToken = default);
    }
}
