using System.Collections.Generic;

namespace Vareiko.Foundation.Environment
{
    public interface IEnvironmentService
    {
        string ActiveProfileId { get; }
        bool Is(string profileId);
        bool TrySetActiveProfile(string profileId);
        bool TryGetString(string key, out string value);
        bool TryGetInt(string key, out int value);
        bool TryGetFloat(string key, out float value);
        bool TryGetBool(string key, out bool value);
        IReadOnlyDictionary<string, string> Snapshot();
    }
}
