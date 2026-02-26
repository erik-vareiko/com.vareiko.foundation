namespace Vareiko.Foundation.UI
{
    public interface IUIValueEventService
    {
        void SetInt(string key, int value);
        void SetFloat(string key, float value);
        void SetBool(string key, bool value);
        void SetString(string key, string value);

        bool TryGetInt(string key, out int value);
        bool TryGetFloat(string key, out float value);
        bool TryGetBool(string key, out bool value);
        bool TryGetString(string key, out string value);
        IReadOnlyValueStream<int> ObserveInt(string key);
        IReadOnlyValueStream<float> ObserveFloat(string key);
        IReadOnlyValueStream<bool> ObserveBool(string key);
        IReadOnlyValueStream<string> ObserveString(string key);

        void Clear(string key);
        void ClearAll();
    }
}
