namespace Vareiko.Foundation.Save
{
    public interface ISaveSerializer
    {
        string Serialize<T>(T model);
        bool TryDeserialize<T>(string raw, out T model);
    }
}
