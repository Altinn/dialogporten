using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser;
using MediatR;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests;

public class ServiceResourceQueriesTests
{
    [Fact]
    public async Task Default_Dispatches_Authorized_Query_With_Parties()
    {
        var sender = new CapturingSender();

        await new Queries().GetServiceResources(
            sender,
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
            acceptLanguage: null,
            parties: ["urn:altinn:organization:identifier-no:111111111"],
            includeUnauthorized: true,
            cancellationToken: TestContext.Current.CancellationToken);

        sender.LastRequest.Should().BeOfType<GetServiceResourceMetadataQuery>();
    }

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
}
