namespace Vareiko.Foundation.Validation
{
    public interface IStartupValidationRule
    {
        string Name { get; }
        StartupValidationResult Validate();
    }
}
