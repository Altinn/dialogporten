using Microsoft.Extensions.Configuration;

namespace Digdir.Domain.Dialogporten.Application;

public sealed class LocalDevelopmentSettings
{
    public const string ConfigurationSectionName = "LocalDevelopment";
    public bool UseLocalDevelopmentUser { get; init; } = true;
    public bool UseLocalDevelopmentResourceRegister { get; init; } = true;
    public bool UseLocalDevelopmentAltinnAuthorization { get; init; } = true;
    public bool UseLocalDevelopmentCloudEventBus { get; init; } = true;
    public bool DisableCache { get; init; } = true;
    public bool DisableAuth { get; init; } = true;
    public bool UseLocalDevelopmentNameRegister { get; set; } = true;
    public bool UseLocalDevelopmentOrganizationRegister { get; set; } = true;
    public bool UseLocalDevelopmentCompactJwsGenerator { get; set; } = true;
    public bool UseInMemoryServiceBusTransport { get; set; } = true;
    public bool DisableSubjectResourceSyncOnStartup { get; set; } = true;
    public bool DisablePolicyInformationSyncOnStartup { get; set; } = true;
    public bool UseLocalDevelopmentPartyNameRegistry { get; set; } = true;
    public bool UseLocalMetricsAggregationStorage { get; set; } = true;
}

public static class LocalDevelopmentSettingsExtensions
{
    public static LocalDevelopmentSettings GetLocalDevelopmentSettings(this IConfiguration configuration) =>
        configuration
            .GetSection(LocalDevelopmentSettings.ConfigurationSectionName)
            .Get<LocalDevelopmentSettings>()
            ?? new();
}
