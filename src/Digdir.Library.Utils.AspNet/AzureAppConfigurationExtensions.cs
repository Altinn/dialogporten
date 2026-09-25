using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;
using Digdir.Domain.Dialogporten.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Digdir.Library.Utils.AspNet;

/// <summary>
/// Wrapper around azure app configuration bootstrapping such that azure app
/// config is activated through the environment variable AZURE_APPCONFIG_URI.
/// </summary>
public static class AzureAppConfigurationExtensions
{
    private const string AzureAppConfigurationUriConfigName = "AZURE_APPCONFIG_URI";
    private const string SentinelKey = "Sentinel";
    private const string DialogDbConnectionStringKey = "Infrastructure:DialogDbConnectionString";

    public static IConfigurationBuilder AddAzureConfiguration(
        this ConfigurationManager config,
        string? environment,
        TokenCredential? credential = null,
        TimeSpan? refreshRate = null)
    {
        if (!config.TryGetAzureAppConfigUri(out var appConfigUri))
        {
            return config;
        }

        credential ??= new DefaultAzureCredential();
        refreshRate ??= TimeSpan.FromMinutes(1);

        return config.AddAzureAppConfiguration(appConfigOptions => appConfigOptions
            .Connect(appConfigUri, credential)
            .ConfigureDialogDatabaseConnection(config)
            .Select(KeyFilter.Any, LabelFilter.Null)
            .SelectIf(!string.IsNullOrWhiteSpace(environment),
                keyFilter: KeyFilter.Any,
                labelFilter: environment!)
            .ConfigureRefresh(refresh => refresh
                .Register(SentinelKey, refreshAll: true)
                .SetRefreshInterval(refreshRate.Value))
            .ConfigureKeyVault(keyVaultOptions => keyVaultOptions
                .SetCredential(credential)
                .SetSecretRefreshInterval(refreshRate.Value)));
    }

    extension(AzureAppConfigurationOptions options)
    {
        internal AzureAppConfigurationOptions ConfigureDialogDatabaseConnection(IConfiguration bootstrapConfiguration)
        {
            if (bootstrapConfiguration.GetValue<DialogDbAuthMode>("Infrastructure:DialogDbAuth:Mode")
                is not DialogDbAuthMode.EntraToken)
            {
                return options;
            }

            var connectionString = bootstrapConfiguration[DialogDbConnectionStringKey];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"{DialogDbConnectionStringKey} must be configured before Azure App Configuration is loaded when using EntraToken authentication.");
            }

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            if (connectionStringBuilder.Password is not null || connectionStringBuilder.Passfile is not null)
            {
                throw new InvalidOperationException(
                    $"{DialogDbConnectionStringKey} must not contain a password or passfile when using EntraToken authentication.");
            }

            // Mapping happens before Key Vault resolution, both on initial load and refresh. Replace
            // the shared administrator reference so this workload never requests its secret.
            return options.Map(setting =>
            {
                if (string.Equals(setting.Key, DialogDbConnectionStringKey, StringComparison.OrdinalIgnoreCase))
                {
                    setting.Value = connectionString;
                    setting.ContentType = null;
                }

                return ValueTask.FromResult(setting);
            });
        }
    }

    public static IApplicationBuilder UseAzureConfiguration(this IApplicationBuilder builder)
    {
        // Check to see if we are targeting an instance of azure app
        // configuration before using azure app configuration middleware.
        return builder.ApplicationServices
            .GetRequiredService<IConfiguration>()
            .TryGetAzureAppConfigUri(out _)
                ? builder.UseAzureAppConfiguration()
                : builder;
    }

    private static AzureAppConfigurationOptions SelectIf(
        this AzureAppConfigurationOptions options,
        bool predicate,
        string keyFilter,
        string labelFilter = "\0")
        => predicate ? options.Select(keyFilter, labelFilter) : options;

    private static bool TryGetAzureAppConfigUri(this IConfiguration config, [NotNullWhen(true)] out Uri? uri)
    {
        var uriAsString = config[AzureAppConfigurationUriConfigName];

        if (string.IsNullOrWhiteSpace(uriAsString))
        {
            uri = null;
            return false;
        }

        if (!Uri.TryCreate(uriAsString, UriKind.Absolute, out uri))
        {
            throw new ArgumentException(
                $"Invalid {AzureAppConfigurationUriConfigName} value: {uriAsString}. " +
                $"Expected null or whitespace for environments not targeting an " +
                $"instance of azure AppConfiguration (usually local development) " +
                $"or a valid absolute uri pointing to an instance of azure " +
                $"AppConfiguration.");
        }

        return true;
    }
}
