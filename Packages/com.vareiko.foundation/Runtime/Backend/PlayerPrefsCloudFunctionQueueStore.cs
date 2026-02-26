using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Backend
{
    public sealed class PlayerPrefsCloudFunctionQueueStore : ICloudFunctionQueueStore
    {
        private const string DefaultStorageKey = "vareiko.foundation.backend.cloud_function_queue";
        private readonly string _storageKey;

        [Inject]
        public PlayerPrefsCloudFunctionQueueStore([InjectOptional] BackendReliabilityConfig config = null)
        {
            _storageKey = config != null && !string.IsNullOrWhiteSpace(config.CloudFunctionQueueStorageKey)
                ? config.CloudFunctionQueueStorageKey.Trim()
                : DefaultStorageKey;
        }

        public IReadOnlyList<CloudFunctionQueueItem> Load()
        {
            if (!PlayerPrefs.HasKey(_storageKey))
            {
                return Array.Empty<CloudFunctionQueueItem>();
            }

            string raw = PlayerPrefs.GetString(_storageKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return Array.Empty<CloudFunctionQueueItem>();
            }

            try
            {
                QueueEnvelope envelope = JsonUtility.FromJson<QueueEnvelope>(raw);
                if (envelope == null || envelope.Items == null || envelope.Items.Count == 0)
                {
                    return Array.Empty<CloudFunctionQueueItem>();
                }

                List<CloudFunctionQueueItem> result = new List<CloudFunctionQueueItem>(envelope.Items.Count);
                for (int i = 0; i < envelope.Items.Count; i++)
                {
                    QueueEntry entry = envelope.Items[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.FunctionName))
                    {
                        continue;
                    }

                    result.Add(new CloudFunctionQueueItem(entry.FunctionName.Trim(), entry.PayloadJson ?? string.Empty));
                }

                return result;
            }
            catch
            {
                return Array.Empty<CloudFunctionQueueItem>();
            }
        }

        public void Save(IReadOnlyList<CloudFunctionQueueItem> queue)
        {
            if (queue == null || queue.Count == 0)
            {
                Clear();
                return;
            }

            QueueEnvelope envelope = new QueueEnvelope
            {
                Items = new List<QueueEntry>(queue.Count)
            };

            for (int i = 0; i < queue.Count; i++)
            {
                CloudFunctionQueueItem item = queue[i];
                if (string.IsNullOrWhiteSpace(item.FunctionName))
                {
                    continue;
                }

                envelope.Items.Add(new QueueEntry
                {
                    FunctionName = item.FunctionName.Trim(),
                    PayloadJson = item.PayloadJson ?? string.Empty
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

        [Serializable]
        private sealed class QueueEnvelope
        {
            public List<QueueEntry> Items = new List<QueueEntry>();
        }

        [Serializable]
        private sealed class QueueEntry
        {
            public string FunctionName;
            public string PayloadJson;
        }
    }
}
