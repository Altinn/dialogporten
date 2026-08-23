using Altinn.AspNet.HealthChecks;
using Digdir.Library.Utils.AspNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.HealthChecks;

public class DialogportenHealthCheckRegistrationTests
{
    private const string AltinnBaseUri = "https://platform.at23.altinn.cloud/";
    private const string ProbeSection = $"WebApi:{DialogportenHealthCheckExtensions.ProbeSectionName}";

    [Fact]
    public void AddDialogportenHealthChecks_Should_Register_Probes_With_Expected_Severity_And_Tags()
    {
        var services = BuildServices(ConfigurationWithProbes(),
        [
            new HealthProbe("Maskinporten", "https://test.maskinporten.no/.well-known")
        ]);

        var registrations = Registrations(services);

        var cdn = Assert.Single(registrations, x => x.Name == "Altinn CDN");
        Assert.Equal(HealthStatus.Degraded, cdn.FailureStatus);
        Assert.Contains(HealthCheckTags.External, cdn.Tags);

        var accessManagement = Assert.Single(registrations, x => x.Name == "Altinn Access Management API");
        Assert.Equal(HealthStatus.Unhealthy, accessManagement.FailureStatus);

        // The JWT metadata endpoints must never fail /health/deep, only degrade it.
        var maskinporten = Assert.Single(registrations, x => x.Name == "Maskinporten");
        Assert.Equal(HealthStatus.Degraded, maskinporten.FailureStatus);
        Assert.Contains(HealthCheckTags.External, maskinporten.Tags);
    }

    [Fact]
    public void AddDialogportenHealthChecks_Should_Be_Idempotent()
    {
        // Infrastructure registers checks through AddAltinnHealthChecks too, and a duplicate name
        // makes HealthCheckService throw for every endpoint, liveness included.
        var services = BuildServices(ConfigurationWithProbes());
        services.AddDialogportenHealthChecks(ConfigurationWithProbes(), ProbeSection);

        var registrations = Registrations(services);

        Assert.Single(registrations, x => x.Name == "self");
        Assert.Equal(
            registrations.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            registrations.Count);

        // Resolving the service is what throws on duplicate names.
        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<HealthCheckService>();
    }

    [Fact]
    public void AddDialogportenHealthChecks_Should_Register_Additional_Probes_Supplied_By_A_Repeated_Call()
    {
        // The section guard must not swallow additional probes: a first call may register the
        // section alone, and a later call may bring the JWT metadata probes. Those must still
        // land on /health/deep, while the section's own probes stay registered exactly once.
        var services = BuildServices(ConfigurationWithProbes());
        services.AddDialogportenHealthChecks(ConfigurationWithProbes(), ProbeSection,
        [
            new HealthProbe("Maskinporten", "https://test.maskinporten.no/.well-known")
        ]);

        // A third call repeating the same additional probe must stay harmless too.
        services.AddDialogportenHealthChecks(ConfigurationWithProbes(), ProbeSection,
        [
            new HealthProbe("Maskinporten", "https://test.maskinporten.no/.well-known")
        ]);

        var registrations = Registrations(services);

        Assert.Single(registrations, x => x.Name == "Maskinporten");
        Assert.Single(registrations, x => x.Name == "Altinn CDN");
        Assert.Equal(
            registrations.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            registrations.Count);

        // Resolving the service is what throws on duplicate names.
        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<HealthCheckService>();
    }

    [Fact]
    public void AddDialogportenHealthChecks_Should_Resolve_RelativePath_Against_Altinn_BaseUri()
    {
        var services = BuildServices(ConfigurationWithProbes());

        // The probe URL is not exposed on the registration, so assert on the resolution failing
        // when - and only when - the base URI is missing.
        Assert.Single(Registrations(services), x => x.Name == "Altinn Access Management API");

        var withoutBaseUri = new ServiceCollection();
        withoutBaseUri.AddLogging();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            withoutBaseUri.AddDialogportenHealthChecks(
                ConfigurationWithProbes(altinnBaseUri: null), ProbeSection));

        Assert.Contains("RelativePath", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDialogportenHealthChecks_Should_Throw_When_Altinn_BaseUri_Is_Not_Absolute()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDialogportenHealthChecks(
                ConfigurationWithProbes(altinnBaseUri: "platform.at23.altinn.cloud"), ProbeSection));

        Assert.Contains("Infrastructure:Altinn:BaseUri", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDialogportenHealthChecks_Should_Throw_When_A_Probe_Name_Is_Duplicated()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDialogportenHealthChecks(ConfigurationWithProbes(), ProbeSection,
            [
                new HealthProbe("Altinn CDN", "https://altinncdn.no/orgs/altinn-orgs.json")
            ]));

        Assert.Contains("Altinn CDN", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddDialogportenHealthChecks_Should_Register_Nothing_But_Self_When_Section_Is_Missing()
    {
        // An environment may legitimately configure no probes; the Service host has no probe
        // section in any appsettings file.
        var services = BuildServices(new ConfigurationBuilder().Build());

        Assert.Single(Registrations(services), x => x.Name == "self");
        Assert.Single(Registrations(services));
    }

    [Fact]
    public void WebApi_Appsettings_Should_Register_Its_Configured_Probes()
    {
        // Pins the shipped configuration to the schema the probes package binds: the probe list is
        // read at registration, so a renamed key or property silently registers nothing (or, for a
        // relative path, fails startup) rather than failing a test elsewhere.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false)
            // Environment-specific, so it lives in appsettings.{env}.json rather than the base file.
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infrastructure:Altinn:BaseUri"] = AltinnBaseUri
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDialogportenHealthChecks(configuration, ProbeSection);

        var registrations = Registrations(services);

        Assert.Equal(HealthStatus.Degraded, Assert.Single(registrations, x => x.Name == "Altinn CDN").FailureStatus);
        Assert.Equal(
            HealthStatus.Unhealthy,
            Assert.Single(registrations, x => x.Name == "Altinn Access Management API").FailureStatus);
    }

    private static ServiceCollection BuildServices(
        IConfiguration configuration,
        IEnumerable<HealthProbe>? additionalProbes = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDialogportenHealthChecks(configuration, ProbeSection, additionalProbes);
        return services;
    }

    private static IReadOnlyList<HealthCheckRegistration> Registrations(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        return [.. provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations];
    }

    private static IConfiguration ConfigurationWithProbes(string? altinnBaseUri = AltinnBaseUri)
    {
        var values = new Dictionary<string, string?>
        {
            [$"{ProbeSection}:0:Name"] = "Altinn CDN",
            [$"{ProbeSection}:0:Url"] = "https://altinncdn.no/orgs/altinn-orgs.json",
            [$"{ProbeSection}:0:Hard"] = "false",
            [$"{ProbeSection}:1:Name"] = "Altinn Access Management API",
            [$"{ProbeSection}:1:RelativePath"] = "accessmanagement/api/v1/meta/info/roles",
            [$"{ProbeSection}:1:Hard"] = "true"
        };

        if (altinnBaseUri is not null)
        {
            values["Infrastructure:Altinn:BaseUri"] = altinnBaseUri;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
