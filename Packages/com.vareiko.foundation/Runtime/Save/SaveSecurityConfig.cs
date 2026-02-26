using UnityEngine;

namespace Vareiko.Foundation.Save
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Save Security Config")]
    public sealed class SaveSecurityConfig : ScriptableObject
    {
        [SerializeField] private bool _enableEncryption;
        [SerializeField] private bool _enableIntegrityHash = true;
        [SerializeField] private bool _allowLegacyPlaintext = true;
        [SerializeField] private bool _enableRollingBackups = true;
        [SerializeField] private int _maxBackupFiles = 3;
        [SerializeField] private bool _restoreFromBackupOnLoadFailure = true;
        [SerializeField] private string _secretKey = "replace-with-project-secret";

        public bool EnableEncryption => _enableEncryption;
        public bool EnableIntegrityHash => _enableIntegrityHash;
        public bool AllowLegacyPlaintext => _allowLegacyPlaintext;
        public bool EnableRollingBackups => _enableRollingBackups;
        public int MaxBackupFiles => _maxBackupFiles < 1 ? 1 : _maxBackupFiles;
        public bool RestoreFromBackupOnLoadFailure => _restoreFromBackupOnLoadFailure;
        public string SecretKey => _secretKey ?? string.Empty;
    }
}
