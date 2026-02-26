using UnityEngine;

namespace Vareiko.Foundation.Config
{
    public interface IConfigService
    {
        void Register<T>(T config, string id = "default") where T : ScriptableObject;
        bool TryGet<T>(out T config, string id = "default") where T : ScriptableObject;
        T GetRequired<T>(string id = "default") where T : ScriptableObject;
        void Unregister<T>(string id = "default") where T : ScriptableObject;
    }
}
