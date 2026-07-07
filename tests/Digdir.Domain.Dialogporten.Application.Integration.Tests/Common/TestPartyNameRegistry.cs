using System.Net;
using System.Net.Http.Json;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry;
using NSubstitute;
using static Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry.IPartyNameRegistryTransport;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public sealed class TestPartyNameRegistry
{
    public static readonly HttpResponseMessage InternalServerError = new()
    {
        Content = null,
        StatusCode = HttpStatusCode.InternalServerError
    };

    public static HttpResponseMessage Ok(NameLookupResult result) => new()
    {
        Content = JsonContent.Create(result),
        StatusCode = HttpStatusCode.OK
    };

    internal IPartyNameRegistryTransport? Override { get; private set; }

    internal void Configure(Action<IPartyNameRegistryTransport> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var substitute = Substitute.For<IPartyNameRegistryTransport>();
        configure(substitute);
        Override = substitute;
    }

    public void Reset() => Override = null;
}

internal sealed class RoutedPartyNameRegistryTransport : IPartyNameRegistryTransport
{
    private readonly TestPartyNameRegistry _testPartyNameRegistry;
    private readonly LocalPartyNameRegistryTransport _fallbackPartyNameRegistryTransport;

    public RoutedPartyNameRegistryTransport(
        TestPartyNameRegistry testPartyNameRegistry,
        LocalPartyNameRegistryTransport fallbackPartyNameRegistryTransport)
    {
        ArgumentNullException.ThrowIfNull(testPartyNameRegistry);
        ArgumentNullException.ThrowIfNull(fallbackPartyNameRegistryTransport);

        _testPartyNameRegistry = testPartyNameRegistry;
        _fallbackPartyNameRegistryTransport = fallbackPartyNameRegistryTransport;
    }

    private IPartyNameRegistryTransport Current =>
        _testPartyNameRegistry.Override ?? _fallbackPartyNameRegistryTransport;

    public Task<HttpResponseMessage> QueryPartyName(
        NameLookup nameLookup,
        CancellationToken cancellationToken) => Current.QueryPartyName(nameLookup, cancellationToken);
}

internal static class TestPartyNameRegistryExtensions
{
    extension<TFlowStep>(TFlowStep flowStep) where TFlowStep : IFlowStep
    {
        public TFlowStep ConfigurePartyNameRegistry(Action<IPartyNameRegistryTransport> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            return flowStep.Do(_ => DialogApplication.PartyNameRegistry.Configure(configure));
        }

        public TFlowStep ResetPartyNameRegistry()
        {
            return flowStep.Do(_ => DialogApplication.PartyNameRegistry.Reset());
        }
    }
}
