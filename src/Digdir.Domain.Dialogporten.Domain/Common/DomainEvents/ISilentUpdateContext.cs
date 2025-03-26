namespace Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;

public interface ISilentUpdateContext
{
    Dictionary<string, string> Metadata { get; }
    void AddMetadata(string key, string value);
}

public static class DomainEventContextExtensions
{
    public static bool IsSilentUpdate(this ISilentUpdateContext context) =>
        context.Metadata.TryGetValue(Constants.IsSilentUpdate, out var value)
        && value == bool.TrueString;
}
