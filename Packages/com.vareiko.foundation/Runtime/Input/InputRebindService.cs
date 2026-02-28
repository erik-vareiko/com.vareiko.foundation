using Zenject;

namespace Vareiko.Foundation.Input
{
    public sealed class InputRebindService : IInputRebindService
    {
        private readonly NewInputSystemAdapter _adapter;

        [Inject]
        public InputRebindService([InjectOptional] NewInputSystemAdapter adapter = null)
        {
            _adapter = adapter;
        }

        public bool IsSupported => _adapter != null && _adapter.SupportsRebinding;

        public bool TryApplyBindingOverride(string actionName, int bindingIndex, string overridePath)
        {
            return _adapter != null && _adapter.TryApplyBindingOverride(actionName, bindingIndex, overridePath);
        }

        public bool TryRemoveBindingOverride(string actionName, int bindingIndex)
        {
            return _adapter != null && _adapter.TryRemoveBindingOverride(actionName, bindingIndex);
        }

        public void ResetAllBindingOverrides()
        {
            _adapter?.ResetAllBindingOverrides();
        }

        public string ExportOverridesJson()
        {
            return _adapter != null ? _adapter.ExportOverridesJson() : string.Empty;
        }

        public bool ImportOverridesJson(string json, bool persist = true)
        {
            return _adapter != null && _adapter.ImportOverridesJson(json, persist);
        }
    }
}
