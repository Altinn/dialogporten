using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser;
using MediatR;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests;

public class ServiceResourceQueriesTests
{
    [Fact]
    public async Task Default_Dispatches_Authorized_Query_With_Parties()
    {
        var sender = new CapturingSender();

        await new Queries().GetServiceResources(
            sender,
            CreateApplicationSettings(enableGraphQlAuthorizedServiceResources: true),
            acceptLanguage: null,
            parties: ["urn:altinn:organization:identifier-no:111111111"],
            includeUnauthorized: false,
            cancellationToken: TestContext.Current.CancellationToken);

        var query = sender.LastRequest.Should().BeOfType<SearchAuthorizedServiceResourcesQuery>().Subject;
        query.Parties.Should().BeEquivalentTo("urn:altinn:organization:identifier-no:111111111");
    }

    [Fact]
    public async Task IncludeUnauthorized_Dispatches_Public_Query()
    {
        var sender = new CapturingSender();

        await new Queries().GetServiceResources(
            sender,
            CreateApplicationSettings(enableGraphQlAuthorizedServiceResources: true),
            acceptLanguage: null,
            parties: ["urn:altinn:organization:identifier-no:111111111"],
            includeUnauthorized: true,
            cancellationToken: TestContext.Current.CancellationToken);

        sender.LastRequest.Should().BeOfType<GetServiceResourceMetadataQuery>();
    }

    [Fact]
    public async Task Disabled_Authorized_Service_Resources_Dispatches_Public_Query()
    {
        var sender = new CapturingSender();

        await new Queries().GetServiceResources(
            sender,
            CreateApplicationSettings(enableGraphQlAuthorizedServiceResources: false),
            acceptLanguage: null,
            parties: ["urn:altinn:organization:identifier-no:111111111"],
            includeUnauthorized: false,
            cancellationToken: TestContext.Current.CancellationToken);

        sender.LastRequest.Should().BeOfType<GetServiceResourceMetadataQuery>();
    }

    private static OptionsMonitorStub<ApplicationSettings> CreateApplicationSettings(
        bool enableGraphQlAuthorizedServiceResources) =>
        new(new ApplicationSettings
        {
            Dialogporten = new DialogportenSettings
            {
                BaseUri = new Uri("https://unit.test"),
                Ed25519KeyPairs = new Ed25519KeyPairs
                {
                    Primary = new Ed25519KeyPair
                    {
                        Kid = "primary",
                        PrivateComponent = "private",
                        PublicComponent = "public"
                    },
                    Secondary = new Ed25519KeyPair
                    {
                        Kid = "secondary",
                        PrivateComponent = "private",
                        PublicComponent = "public"
                    }
                }
            },
            FeatureToggle = new FeatureToggle
            {
                EnableGraphQlAuthorizedServiceResources = enableGraphQlAuthorizedServiceResources
            }
        });

    private sealed class CapturingSender : ISender
    {
        public object? LastRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            object response = request switch
            {
                SearchAuthorizedServiceResourcesQuery => new SearchAuthorizedServiceResourcesDto(),
                GetServiceResourceMetadataQuery => new GetServiceResourceMetadataDto(),
                _ => throw new NotSupportedException()
            };
            return Task.FromResult((TResponse)response);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class OptionsMonitorStub<TOptions>(TOptions value) : IOptionsMonitor<TOptions>
    {
        public TOptions CurrentValue => value;

        public TOptions Get(string? name) => value;

        public IDisposable OnChange(Action<TOptions, string?> listener) => NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
