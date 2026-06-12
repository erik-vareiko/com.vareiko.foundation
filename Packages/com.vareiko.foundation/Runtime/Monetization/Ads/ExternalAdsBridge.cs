using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Ads
{
    public static class ExternalAdsBridge
    {
        private static Func<CancellationToken, UniTask<AdsInitializeResult>> _initializeHandler;
        private static Func<string, CancellationToken, UniTask<AdLoadResult>> _loadHandler;
        private static Func<string, CancellationToken, UniTask<AdShowResult>> _showHandler;

        public static void SetInitializeHandler(Func<CancellationToken, UniTask<AdsInitializeResult>> handler)
        {
            _initializeHandler = handler;
        }

        public static void SetLoadHandler(Func<string, CancellationToken, UniTask<AdLoadResult>> handler)
        {
            _loadHandler = handler;
        }

        public static void SetShowHandler(Func<string, CancellationToken, UniTask<AdShowResult>> handler)
        {
            _showHandler = handler;
        }

        public static void ClearHandlers()
        {
            _initializeHandler = null;
            _loadHandler = null;
            _showHandler = null;
        }

        internal static bool TryGetInitializeHandler(out Func<CancellationToken, UniTask<AdsInitializeResult>> handler)
        {
            handler = _initializeHandler;
            return handler != null;
        }

        internal static bool TryGetLoadHandler(out Func<string, CancellationToken, UniTask<AdLoadResult>> handler)
        {
            handler = _loadHandler;
            return handler != null;
        }

        internal static bool TryGetShowHandler(out Func<string, CancellationToken, UniTask<AdShowResult>> handler)
        {
            handler = _showHandler;
            return handler != null;
        }
    }
}
