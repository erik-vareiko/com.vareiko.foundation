using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Vareiko.Foundation.Save
{
    public sealed class PlayerPrefsSaveStorage : ISaveStorage
    {
        private const string DefaultKeyPrefix = "Vareiko.Foundation.Save.";

        private readonly string _rootPath;
        private readonly string _keyPrefix;

        public PlayerPrefsSaveStorage(
            string rootPath = null,
            string keyPrefix = null)
        {
            _rootPath = NormalizePath(rootPath);
            _keyPrefix = string.IsNullOrWhiteSpace(keyPrefix) ? DefaultKeyPrefix : keyPrefix;
        }

        public async UniTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return PlayerPrefs.HasKey(BuildKey(path));
        }

        public async UniTask<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            string key = BuildKey(path);
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key, string.Empty) : null;
        }

        public async UniTask WriteTextAsync(string path, string value, CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            PlayerPrefs.SetString(BuildKey(path), value ?? string.Empty);
            PlayerPrefs.Save();
        }

        public async UniTask DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            string key = BuildKey(path);
            if (!PlayerPrefs.HasKey(key))
            {
                return;
            }

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        private string BuildKey(string path)
        {
            string normalizedPath = NormalizePath(path);
            string relativePath = normalizedPath;

            if (!string.IsNullOrEmpty(_rootPath))
            {
                if (string.Equals(normalizedPath, _rootPath, StringComparison.Ordinal))
                {
                    relativePath = string.Empty;
                }
                else
                {
                    string rootPrefix = _rootPath + "/";
                    if (normalizedPath.StartsWith(rootPrefix, StringComparison.Ordinal))
                    {
                        relativePath = normalizedPath.Substring(rootPrefix.Length);
                    }
                }
            }

            relativePath = relativePath.Trim('/');
            if (string.IsNullOrEmpty(relativePath))
            {
                relativePath = "root";
            }

            return _keyPrefix + relativePath;
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/').TrimEnd('/');
        }
    }
}
