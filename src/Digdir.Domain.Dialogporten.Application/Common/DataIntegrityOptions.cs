namespace Digdir.Domain.Dialogporten.Application.Common;

public enum BadDataHandling
{
    WarnAndContinue,
    Throw
}

public sealed class DataIntegrityOptions
{
    public const string ConfigurationSectionName = "DataIntegrity";
    public BadDataHandling BadUserResourceDataHandling { get; init; } = BadDataHandling.Throw;
}
