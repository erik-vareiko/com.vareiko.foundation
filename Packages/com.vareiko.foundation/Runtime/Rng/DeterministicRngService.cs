using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.Rng
{
    public sealed class DeterministicRngService : IDeterministicRngService
    {
        private readonly DeterministicRngConfig _config;
        private readonly Dictionary<string, DeterministicRngStream> _streams = new Dictionary<string, DeterministicRngStream>(StringComparer.Ordinal);

        private int _rootSeed;
        private bool _initialized;

        public DeterministicRngService(DeterministicRngConfig config = null)
        {
            _config = config;
            _rootSeed = _config != null ? _config.DefaultRootSeed : 1;
        }

        public void Initialize(int rootSeed)
        {
            if (_initialized && _config != null && !_config.AllowReseedAtRuntime)
            {
                return;
            }

            _rootSeed = rootSeed;
            _streams.Clear();
            _initialized = true;
        }

        public IDeterministicRngStream CreateStream(string scope)
        {
            string normalizedScope = NormalizeScope(scope);
            EnsureInitialized();

            DeterministicRngStream stream;
            if (!_streams.TryGetValue(normalizedScope, out stream))
            {
                RngStreamState state = BuildInitialState(normalizedScope);
                stream = new DeterministicRngStream(new Pcg32(state));
                _streams[normalizedScope] = stream;
            }

            return stream;
        }

        public IDeterministicRngStream RestoreStream(string scope, RngStreamState state)
        {
            string normalizedScope = NormalizeScope(scope);
            EnsureInitialized();

            DeterministicRngStream stream = new DeterministicRngStream(new Pcg32(state));
            _streams[normalizedScope] = stream;
            return stream;
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            Initialize(_rootSeed);
        }

        private RngStreamState BuildInitialState(string scope)
        {
            uint mixedSeed = MixSeed(_rootSeed, scope);
            ulong sequence = DeriveSequence(scope);
            Pcg32 generator = new Pcg32(mixedSeed, sequence);
            RngStreamState state = generator.CaptureState();

            if (_config != null && _config.EnableRngDiagnostics)
            {
                Debug.Log($"DeterministicRngService: stream created scope={scope} seed={_rootSeed}.");
            }

            return state;
        }

        private static uint MixSeed(int rootSeed, string scope)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash ^= (uint)rootSeed;
                hash *= 16777619u;

                for (int i = 0; i < scope.Length; i++)
                {
                    hash ^= scope[i];
                    hash *= 16777619u;
                }

                return hash;
            }
        }

        private static ulong DeriveSequence(string scope)
        {
            unchecked
            {
                ulong hash = 1469598103934665603ul;
                for (int i = 0; i < scope.Length; i++)
                {
                    hash ^= scope[i];
                    hash *= 1099511628211ul;
                }

                return hash == 0ul ? 1ul : hash;
            }
        }

        private static string NormalizeScope(string scope)
        {
            return string.IsNullOrWhiteSpace(scope) ? "default" : scope.Trim();
        }
    }
}
