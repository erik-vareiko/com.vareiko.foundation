using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vareiko.Foundation.Backend
{
    public sealed class PlayerPrefsCloudCommandQueueStore : ICloudCommandQueueStore
    {
        private const string DefaultStorageKey = "vareiko.foundation.backend.cloud_command_queue";
        private const string DefaultLegacyKey = "vareiko.foundation.backend.cloud_function_queue";

        private readonly string _storageKey;
        private readonly string _legacyStorageKey;

        public PlayerPrefsCloudCommandQueueStore(
            BackendReliabilityConfig reliabilityConfig = null)
        {
            _storageKey = DefaultStorageKey;
            _legacyStorageKey = reliabilityConfig != null && !string.IsNullOrWhiteSpace(reliabilityConfig.CloudFunctionQueueStorageKey)
                ? reliabilityConfig.CloudFunctionQueueStorageKey.Trim()
                : DefaultLegacyKey;
        }

        public IReadOnlyList<CloudCommandQueueItem> Load()
        {
            IReadOnlyList<CloudCommandQueueItem> current = TryLoadCurrent();
            if (current.Count > 0)
            {
                return current;
            }

            IReadOnlyList<CloudCommandQueueItem> migrated = TryLoadLegacyAndMigrate();
            return migrated;
        }

        public void Save(IReadOnlyList<CloudCommandQueueItem> queue)
        {
            if (queue == null || queue.Count == 0)
            {
                Clear();
                return;
            }

            QueueEnvelopeV2 envelope = new QueueEnvelopeV2
            {
                Version = 2,
                Items = new List<QueueEntryV2>(queue.Count)
            };

            for (int i = 0; i < queue.Count; i++)
            {
                CloudCommandQueueItem item = queue[i];
                if (string.IsNullOrWhiteSpace(item.FunctionName))
                {
                    continue;
                }

                envelope.Items.Add(new QueueEntryV2
                {
                    FunctionName = item.FunctionName.Trim(),
                    PayloadJson = item.PayloadJson ?? string.Empty,
                    RequestJson = item.RequestJson ?? string.Empty,
                    IdempotencyKey = item.IdempotencyKey ?? string.Empty,
                    AttemptCount = item.AttemptCount < 0 ? 0 : item.AttemptCount,
                    FirstQueuedUnixMs = item.FirstQueuedUnixMs,
                    LastAttemptUnixMs = item.LastAttemptUnixMs
                });
            }

            if (envelope.Items.Count == 0)
            {
                Clear();
                return;
            }

            PlayerPrefs.SetString(_storageKey, JsonUtility.ToJson(envelope));
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            if (PlayerPrefs.HasKey(_storageKey))
            {
                PlayerPrefs.DeleteKey(_storageKey);
                PlayerPrefs.Save();
            }
        }

        private IReadOnlyList<CloudCommandQueueItem> TryLoadCurrent()
        {
            if (!PlayerPrefs.HasKey(_storageKey))
            {
                return Array.Empty<CloudCommandQueueItem>();
            }

            string raw = PlayerPrefs.GetString(_storageKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return Array.Empty<CloudCommandQueueItem>();
            }

            try
            {
                QueueEnvelopeV2 envelope = JsonUtility.FromJson<QueueEnvelopeV2>(raw);
                if (envelope == null || envelope.Items == null || envelope.Items.Count == 0)
                {
                    return Array.Empty<CloudCommandQueueItem>();
                }

                List<CloudCommandQueueItem> result = new List<CloudCommandQueueItem>(envelope.Items.Count);
                for (int i = 0; i < envelope.Items.Count; i++)
                {
                    QueueEntryV2 entry = envelope.Items[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.FunctionName))
                    {
                        Debug.LogWarning("PlayerPrefsCloudCommandQueueStore: Skipping invalid v2 queue entry.");
                        continue;
                    }

                    result.Add(new CloudCommandQueueItem(
                        envelope.Version,
                        entry.FunctionName.Trim(),
                        entry.PayloadJson ?? string.Empty,
                        entry.RequestJson ?? string.Empty,
                        entry.IdempotencyKey ?? string.Empty,
                        entry.AttemptCount,
                        entry.FirstQueuedUnixMs,
                        entry.LastAttemptUnixMs));
                }

                return result;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"PlayerPrefsCloudCommandQueueStore: Failed to parse v2 queue. {exception.Message}");
                return Array.Empty<CloudCommandQueueItem>();
            }
        }

        private IReadOnlyList<CloudCommandQueueItem> TryLoadLegacyAndMigrate()
        {
            if (!PlayerPrefs.HasKey(_legacyStorageKey))
            {
                return Array.Empty<CloudCommandQueueItem>();
            }

            string raw = PlayerPrefs.GetString(_legacyStorageKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return Array.Empty<CloudCommandQueueItem>();
            }

            try
            {
                LegacyQueueEnvelope envelope = JsonUtility.FromJson<LegacyQueueEnvelope>(raw);
                if (envelope == null || envelope.Items == null || envelope.Items.Count == 0)
                {
                    return Array.Empty<CloudCommandQueueItem>();
                }

                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                List<CloudCommandQueueItem> result = new List<CloudCommandQueueItem>(envelope.Items.Count);
                for (int i = 0; i < envelope.Items.Count; i++)
                {
                    LegacyQueueEntry entry = envelope.Items[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.FunctionName))
                    {
                        Debug.LogWarning("PlayerPrefsCloudCommandQueueStore: Skipping invalid legacy queue entry.");
                        continue;
                    }

                    result.Add(CloudCommandQueueItem.Create(
                        entry.FunctionName.Trim(),
                        entry.PayloadJson ?? string.Empty,
                        string.Empty,
                        string.Empty,
                        now));
                }

                if (result.Count > 0)
                {
                    Save(result);
                }

                PlayerPrefs.DeleteKey(_legacyStorageKey);
                PlayerPrefs.Save();

                return result;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"PlayerPrefsCloudCommandQueueStore: Failed to parse legacy queue. {exception.Message}");
                return Array.Empty<CloudCommandQueueItem>();
            }
        }

        [Serializable]
        private sealed class QueueEnvelopeV2
        {
            public int Version = 2;
            public List<QueueEntryV2> Items = new List<QueueEntryV2>();
        }

        [Serializable]
        private sealed class QueueEntryV2
        {
            public string FunctionName;
            public string PayloadJson;
            public string RequestJson;
            public string IdempotencyKey;
            public int AttemptCount;
            public long FirstQueuedUnixMs;
            public long LastAttemptUnixMs;
        }

        [Serializable]
        private sealed class LegacyQueueEnvelope
        {
            public List<LegacyQueueEntry> Items = new List<LegacyQueueEntry>();
        }

        [Serializable]
        private sealed class LegacyQueueEntry
        {
            public string FunctionName;
            public string PayloadJson;
        }
    }
}
