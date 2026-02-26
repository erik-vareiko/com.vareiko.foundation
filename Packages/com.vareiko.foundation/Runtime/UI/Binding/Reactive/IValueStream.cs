namespace Vareiko.Foundation.UI
{
    public interface IValueStream<T> : IReadOnlyValueStream<T>
    {
        void SetValue(T value);
        void Clear();
    }
}
