namespace Vareiko.Foundation.Config
{
    public readonly struct ConfigRegisteredSignal
    {
        public readonly string Id;
        public readonly string TypeName;

        public ConfigRegisteredSignal(string id, string typeName)
        {
            Id = id;
            TypeName = typeName;
        }
    }

    public readonly struct ConfigMissingSignal
    {
        public readonly string Id;
        public readonly string TypeName;

        public ConfigMissingSignal(string id, string typeName)
        {
            Id = id;
            TypeName = typeName;
        }
    }
}
