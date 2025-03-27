using Digdir.Domain.Dialogporten.Domain.Common;

namespace Digdir.Domain.Dialogporten.Application.Common.Context;

public interface IApplicationContext
{
    Dictionary<string, string> Metadata { get; }
    void AddMetadata(string key, string value);
}

public static class ApplicationContextExtensions
{
    public static bool IsSilentUpdate(this IApplicationContext context) =>
        context.Metadata.TryGetValue(Constants.IsSilentUpdate, out var value)
        && value == bool.TrueString;
}
