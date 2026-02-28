namespace Vareiko.Foundation.Input
{
    public interface IInputRebindService
    {
        bool IsSupported { get; }
        bool TryApplyBindingOverride(string actionName, int bindingIndex, string overridePath);
        bool TryRemoveBindingOverride(string actionName, int bindingIndex);
        void ResetAllBindingOverrides();
        string ExportOverridesJson();
        bool ImportOverridesJson(string json, bool persist = true);
    }
}
