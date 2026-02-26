using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Consent
{
    public interface IConsentService
    {
        bool IsLoaded { get; }
        bool IsConsentCollected { get; }
        bool HasConsent(ConsentScope scope);
        UniTask LoadAsync(CancellationToken cancellationToken = default);
        UniTask SaveAsync(CancellationToken cancellationToken = default);
        void SetConsent(ConsentScope scope, bool granted, bool saveImmediately = false);
        void SetConsentCollected(bool isCollected, bool saveImmediately = false);
    }
}
