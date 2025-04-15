namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;

internal interface IRequireDataLoader
{
    Dictionary<string, object?> PreloadedData { get; }
}
