using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Attribution
{
    public static class ExternalAttributionBridge
    {
        private static Func<CancellationToken, UniTask<AttributionInitializeResult>> _initializeHandler;
        private static Action<string> _setUserIdHandler;
        private static Func<string, IReadOnlyDictionary<string, string>, CancellationToken, UniTask<AttributionTrackResult>> _trackEventHandler;
        private static Func<AttributionRevenueData, CancellationToken, UniTask<AttributionRevenueTrackResult>> _trackRevenueHandler;

        public static void SetInitializeHandler(Func<CancellationToken, UniTask<AttributionInitializeResult>> handler)
        {
            _initializeHandler = handler;
        }

        public static void SetUserIdHandler(Action<string> handler)
        {
            _setUserIdHandler = handler;
        }

        public static void SetTrackEventHandler(Func<string, IReadOnlyDictionary<string, string>, CancellationToken, UniTask<AttributionTrackResult>> handler)
        {
            _trackEventHandler = handler;
        }

        public static void SetTrackRevenueHandler(Func<AttributionRevenueData, CancellationToken, UniTask<AttributionRevenueTrackResult>> handler)
        {
            _trackRevenueHandler = handler;
        }

        public static void ClearHandlers()
        {
            _initializeHandler = null;
            _setUserIdHandler = null;
            _trackEventHandler = null;
            _trackRevenueHandler = null;
        }

        internal static bool TryGetInitializeHandler(out Func<CancellationToken, UniTask<AttributionInitializeResult>> handler)
        {
            handler = _initializeHandler;
            return handler != null;
        }

        internal static bool TryGetSetUserIdHandler(out Action<string> handler)
        {
            handler = _setUserIdHandler;
            return handler != null;
        }

        internal static bool TryGetTrackEventHandler(out Func<string, IReadOnlyDictionary<string, string>, CancellationToken, UniTask<AttributionTrackResult>> handler)
        {
            handler = _trackEventHandler;
            return handler != null;
        }

        internal static bool TryGetTrackRevenueHandler(out Func<AttributionRevenueData, CancellationToken, UniTask<AttributionRevenueTrackResult>> handler)
        {
            handler = _trackRevenueHandler;
            return handler != null;
        }
    }
}
