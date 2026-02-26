namespace Vareiko.Foundation.UI
{
    public readonly struct UIIntValueChangedSignal
    {
        public readonly string Key;
        public readonly int Value;

        public UIIntValueChangedSignal(string key, int value)
        {
            Key = key ?? string.Empty;
            Value = value;
        }
    }

    public readonly struct UIFloatValueChangedSignal
    {
        public readonly string Key;
        public readonly float Value;

        public UIFloatValueChangedSignal(string key, float value)
        {
            Key = key ?? string.Empty;
            Value = value;
        }
    }

    public readonly struct UIBoolValueChangedSignal
    {
        public readonly string Key;
        public readonly bool Value;

        public UIBoolValueChangedSignal(string key, bool value)
        {
            Key = key ?? string.Empty;
            Value = value;
        }
    }

    public readonly struct UIStringValueChangedSignal
    {
        public readonly string Key;
        public readonly string Value;

        public UIStringValueChangedSignal(string key, string value)
        {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
        }
    }
}
