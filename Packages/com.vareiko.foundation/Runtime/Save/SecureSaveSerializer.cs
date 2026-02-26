using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Save
{
    public sealed class SecureSaveSerializer : ISaveSerializer
    {
        private readonly JsonUnitySaveSerializer _inner;
        private readonly SaveSecurityConfig _config;

        [Inject]
        public SecureSaveSerializer(JsonUnitySaveSerializer inner, [InjectOptional] SaveSecurityConfig config = null)
        {
            _inner = inner;
            _config = config;
        }

        public string Serialize<T>(T model)
        {
            string payload = _inner.Serialize(model);
            if (!IsSecurityEnabled())
            {
                return payload;
            }

            bool isEncrypted = _config.EnableEncryption;
            string data = isEncrypted ? Encrypt(payload) : payload;
            string hash = _config.EnableIntegrityHash ? ComputeHash(payload) : string.Empty;

            SecureEnvelope envelope = new SecureEnvelope
            {
                Version = 1,
                IsEncrypted = isEncrypted,
                Payload = data,
                Hash = hash
            };

            return JsonUtility.ToJson(envelope);
        }

        public bool TryDeserialize<T>(string raw, out T model)
        {
            model = default;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            SecureEnvelope envelope;
            if (TryParseEnvelope(raw, out envelope))
            {
                string payload = envelope.Payload ?? string.Empty;
                if (envelope.IsEncrypted)
                {
                    string decrypted;
                    if (!TryDecrypt(payload, out decrypted))
                    {
                        return false;
                    }

                    payload = decrypted;
                }

                if (!ValidateIntegrity(payload, envelope.Hash))
                {
                    return false;
                }

                return _inner.TryDeserialize(payload, out model);
            }

            if (_config == null || _config.AllowLegacyPlaintext)
            {
                return _inner.TryDeserialize(raw, out model);
            }

            return false;
        }

        private bool IsSecurityEnabled()
        {
            if (_config == null)
            {
                return false;
            }

            return _config.EnableEncryption || _config.EnableIntegrityHash;
        }

        private bool ValidateIntegrity(string payload, string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return true;
            }

            string expected = ComputeHash(payload);
            return string.Equals(expected, hash, StringComparison.Ordinal);
        }

        private bool TryParseEnvelope(string raw, out SecureEnvelope envelope)
        {
            envelope = null;
            try
            {
                envelope = JsonUtility.FromJson<SecureEnvelope>(raw);
            }
            catch (Exception)
            {
                return false;
            }

            if (envelope == null || envelope.Version < 1 || string.IsNullOrEmpty(envelope.Payload))
            {
                return false;
            }

            return true;
        }

        private string Encrypt(string payload)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(payload ?? string.Empty);
            byte[] key = GetKeyBytes();
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ key[i % key.Length]);
            }

            return Convert.ToBase64String(bytes);
        }

        private bool TryDecrypt(string encryptedPayload, out string payload)
        {
            payload = string.Empty;
            if (string.IsNullOrWhiteSpace(encryptedPayload))
            {
                return false;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(encryptedPayload);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] key = GetKeyBytes();
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ key[i % key.Length]);
            }

            payload = Encoding.UTF8.GetString(bytes);
            return true;
        }

        private string ComputeHash(string payload)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string salted = (_config != null ? _config.SecretKey : string.Empty) + "|" + (payload ?? string.Empty);
                byte[] bytes = Encoding.UTF8.GetBytes(salted);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private byte[] GetKeyBytes()
        {
            string key = _config != null ? _config.SecretKey : string.Empty;
            if (string.IsNullOrEmpty(key))
            {
                key = "foundation-default-key";
            }

            byte[] bytes = Encoding.UTF8.GetBytes(key);
            if (bytes.Length == 0)
            {
                return new byte[] { 1 };
            }

            return bytes;
        }

        [Serializable]
        private sealed class SecureEnvelope
        {
            public int Version;
            public bool IsEncrypted;
            public string Payload;
            public string Hash;
        }
    }
}
