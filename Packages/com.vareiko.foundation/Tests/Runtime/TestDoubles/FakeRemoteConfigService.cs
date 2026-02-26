using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Backend;

namespace Vareiko.Foundation.Tests.TestDoubles
{
    public sealed class FakeRemoteConfigService : IRemoteConfigService
    {
        private readonly Dictionary<string, string> _values;

        public FakeRemoteConfigService(Dictionary<string, string> values)
        {
            _values = values ?? new Dictionary<string, string>(0, System.StringComparer.Ordinal);
        }

        public bool IsReady => true;

        public UniTask RefreshAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        public bool TryGetString(string key, out string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = string.Empty;
                return false;
            }

            return _values.TryGetValue(key, out value);
        }

        public bool TryGetInt(string key, out int value)
        {
            value = default;
            string raw;
            if (!TryGetString(key, out raw))
            {
                return false;
            }

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public bool TryGetFloat(string key, out float value)
        {
            value = default;
            string raw;
            if (!TryGetString(key, out raw))
            {
                return false;
            }

            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public IReadOnlyDictionary<string, string> Snapshot()
        {
            return _values;
        }
    }
}
