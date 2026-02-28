namespace Vareiko.Foundation.Observability
{
    public interface IMonetizationObservabilityService
    {
        MonetizationObservabilitySnapshot Snapshot { get; }
    }
}
