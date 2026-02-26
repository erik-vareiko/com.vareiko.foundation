using UnityEngine;

namespace Vareiko.Foundation.Save
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Save Security Config")]
    public sealed class SaveSecurityConfig : ScriptableObject
    {
        [SerializeField] private bool _enableEncryption;
        [SerializeField] private bool _enableIntegrityHash = true;
        [SerializeField] private bool _allowLegacyPlaintext = true;
        [SerializeField] private string _secretKey = "replace-with-project-secret";

        public bool EnableEncryption => _enableEncryption;
        public bool EnableIntegrityHash => _enableIntegrityHash;
        public bool AllowLegacyPlaintext => _allowLegacyPlaintext;
        public string SecretKey => _secretKey ?? string.Empty;
    }
}
