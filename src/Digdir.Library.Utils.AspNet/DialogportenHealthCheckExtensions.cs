using Altinn.AspNet.HealthChecks;
using Altinn.AspNet.HealthChecks.Probes;
using Digdir.Domain.Dialogporten.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Digdir.Library.Utils.AspNet;

public static class DialogportenHealthCheckExtensions
{
    /// <summary>
    /// The configuration key holding the outbound probe list, relative to the host's own settings
    /// section (<c>WebApi:HealthProbes</c>, <c>GraphQl:HealthProbes</c>) or at the root for hosts
    /// without one.
    /// </summary>
    public const string ProbeSectionName = "HealthProbes";

    /// <summary>
    /// The liveness path. Pinned rather than taken from the library, which defaults to
    /// <c>/alive</c>: the Container Apps liveness probe in
    /// <c>.azure/modules/containerApp/main.bicep</c> points at <c>/health/liveness</c>, and a probe
    /// path is a deployment contract, not an implementation detail.
    /// </summary>
    public const string LivenessPath = "/health/liveness";

    private const string AltinnBaseUriKey =
        $"{InfrastructureSettings.ConfigurationSectionName}:" +
        $"{nameof(InfrastructureSettings.Altinn)}:{nameof(AltinnPlatformSettings.BaseUri)}";

    /// <summary>
    /// Registers the Altinn health check convention plus one outbound HTTP probe per entry in
    /// <paramref name="probeSectionPath"/>, tagged "external" so the probes surface on
    /// /health/deep only. Entries using <c>RelativePath</c> resolve against
    /// <c>Infrastructure:Altinn:BaseUri</c>.
    /// </summary>
    /// <remarks>
    /// The probe list is bound here rather than through <c>IOptions</c> because each probe needs its
    /// own health check registration, and there is no service provider yet. The values are frozen at
    /// startup.
    /// </remarks>
    public static IServiceCollection AddDialogportenHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration,
        string probeSectionPath,
        IEnumerable<HealthProbe>? additionalProbes = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(probeSectionPath);

        // Idempotent - Infrastructure calls this too. Both call sites append to the same
        // registration list, so neither needs to know about the other.
        var healthChecks = services.AddAltinnHealthChecks();

        // Registering the same section twice would register duplicate probe names, which the probes
        // package rejects at registration. Guard the way AddAltinnHealthChecks guards itself, so a
        // repeated call stays harmless rather than failing startup.
        var alreadyRegistered = services
            .Select(x => x.ImplementationInstance)
            .OfType<OutboundProbeMarker>()
            .Any(x => string.Equals(x.SectionName, probeSectionPath, StringComparison.OrdinalIgnoreCase));

        if (alreadyRegistered)
        {
            return services;
        }

        services.AddSingleton(new OutboundProbeMarker(probeSectionPath));

        healthChecks.AddOutboundProbes(
            configuration.GetSection(probeSectionPath),
            // Only consulted by entries using RelativePath; the package names the offending
            // configuration path if one needs it and it is missing.
            probes => probes.BaseUri = ResolveAltinnBaseUri(configuration));

        foreach (var probe in additionalProbes ?? [])
        {
            healthChecks.AddOutboundProbe(probe.Name, ParseProbeUrl(probe), probe.Hard);
        }

        return services;
    }

    /// <summary>
    /// Maps the Altinn health check endpoints, with exception details and entry data in the response
    /// body only in development.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The health endpoints are publicly reachable - through APIM for WebApi and GraphQL, and
    /// directly on the container app ingress - so the body is trimmed to status, name, tags and the
    /// descriptions of checks that did not throw outside development.
    /// </para>
    /// <para>
    /// Exception messages from Npgsql, Redis and the outbound probes routinely carry hostnames and
    /// connection strings. Suppressing them also drops the description of a check that threw: the
    /// health check service uses the exception message as the description, so suppressing only the
    /// one field would still leak it.
    /// </para>
    /// <para>
    /// Entry data needs suppressing for a different reason - it is published while everything is
    /// healthy. MassTransit's bus-state check reports the Service Bus host address and the queue
    /// names it knows, and MassTransit offers no way to trim that at the source.
    /// </para>
    /// <para>
    /// The detail level is set explicitly rather than left to the library's environment-derived
    /// default. That default only recognises the literal name "Production"; our container apps run
    /// with ASPNETCORE_ENVIRONMENT set to prod, staging, test or yt01, every one of which would fall
    /// through to <see cref="HealthReportDetailLevel.Diagnostic"/> and publish exactly the exception
    /// messages and entry data described above.
    /// </para>
    /// </remarks>
    public static WebApplication MapDialogportenHealthChecks(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var isDevelopment = app.Environment.IsDevelopment();

        var options = CreateEndpointOptions();

        options.DetailLevel = isDevelopment
            ? HealthReportDetailLevel.Full
            : HealthReportDetailLevel.Summary;

        app.MapAltinnHealthChecks(options);

        return app;
    }

    /// <summary>
    /// The health endpoint layout: the library's defaults with <see cref="LivenessPath"/> pinned.
    /// </summary>
    /// <remarks>
    /// Called once for the endpoint mapping and once for OpenTelemetry trace suppression, which
    /// needs the same paths to know which spans to drop. Two instances rather than one shared one:
    /// the values are identical either way, and a per-call instance keeps the paths immutable in
    /// practice and does not have to outlive the host - hosts are per-process, but the test suite
    /// builds several in one.
    /// </remarks>
    internal static HealthCheckEndpointOptions CreateEndpointOptions()
    {
        var options = new HealthCheckEndpointOptions();
        options.Liveness.Path = LivenessPath;
        return options;
    }

    private static Uri ParseProbeUrl(HealthProbe probe) =>
        Uri.TryCreate(probe.Url, UriKind.Absolute, out var uri)
            ? uri
            : throw new InvalidOperationException(
                $"Health probe '{probe.Name}' has URL '{probe.Url}', which is not an absolute URI.");

    // The Altinn base URI differs per environment and is validated as part of InfrastructureSettings,
    // but that validation runs later than health check registration, so a bad value has to be caught
    // here rather than trusted.
    private static Uri? ResolveAltinnBaseUri(IConfiguration configuration)
    {
        var configured = configuration[AltinnBaseUriKey];

        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        return Uri.TryCreate(configured, UriKind.Absolute, out var uri)
            ? uri
            : throw new InvalidOperationException(
                $"'{AltinnBaseUriKey}' must be an absolute URI to resolve relative health probe " +
                $"paths, but was '{configured}'.");
    }

    private sealed record OutboundProbeMarker(string SectionName);
}
