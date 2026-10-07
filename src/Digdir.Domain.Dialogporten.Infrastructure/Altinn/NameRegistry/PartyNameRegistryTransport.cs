using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Parties;
using static System.StringComparison;
using static Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry.IPartyNameRegistryTransport;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry;

internal interface IPartyNameRegistryTransport
{
    Task<HttpResponseMessage> QueryPartyNameResponse(
        NameLookup nameLookup,
        CancellationToken cancellationToken
    );
    Task<NameLookupResult> QueryPartyName(
        NameLookup nameLookup,
        CancellationToken cancellationToken
    );

    internal sealed class NameLookup
    {
        public List<string> Data { get; set; } = null!;
    }

    internal sealed class NameLookupResult
    {
        public List<NameLookupEntry> Data { get; set; } = null!;
    }

    internal sealed class NameLookupEntry
    {
        public string? DisplayName { get; set; }
    }
}

internal sealed class PartyNameRegistryTransport : IPartyNameRegistryTransport
{
    private readonly HttpClient _client;
    public const string QueryPartiesUrl = "register/api/v1/dialogporten/parties/query";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public PartyNameRegistryTransport(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    public async Task<NameLookupResult> QueryPartyName(
        NameLookup nameLookup,
        CancellationToken cancellationToken
    )
    {
        var response = await _client.PostAsJsonEnsuredAsync(
            QueryPartiesUrl,
            nameLookup,
            serializerOptions: SerializerOptions,
            cancellationToken: cancellationToken
        );

        return await response.Content.ReadFromJsonAsync<NameLookupResult>(cancellationToken) ?? throw new JsonException(
            $"Failed to deserialize JSON to type {typeof(NameLookupResult).FullName} from {QueryPartiesUrl}"
        );
    }

    public async Task<HttpResponseMessage> QueryPartyNameResponse(
        NameLookup nameLookup,
        CancellationToken cancellationToken
    )
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, QueryPartiesUrl)
        {
            Content = JsonContent.Create(nameLookup, options: SerializerOptions)
        };

        return await _client.SendAsync(httpRequestMessage, cancellationToken);
    }
}

internal sealed class LocalPartyNameRegistryTransport : IPartyNameRegistryTransport
{
    public Task<HttpResponseMessage> QueryPartyNameResponse(
        NameLookup nameLookup,
        CancellationToken cancellationToken
    )
    {
        var name = nameLookup switch
        {
            var x when x.Data
                .Single()
                .StartsWith(SystemUserIdentifier.PrefixWithSeparator, InvariantCulture) => "Systembruker",
            _ => "Brando Sando"
        };

        return Task.FromResult(new HttpResponseMessage
        {
            Content = JsonContent.Create(new NameLookupResult
            {
                Data =
                [
                    new NameLookupEntry
                    {
                        DisplayName = name
                    }
                ]
            }),
            StatusCode = HttpStatusCode.OK
        });
    }

    public async Task<NameLookupResult> QueryPartyName(
        NameLookup nameLookup,
        CancellationToken cancellationToken
    )
    {
        var response = await QueryPartyNameResponse(nameLookup, cancellationToken);
        return await response
            .Content
            .ReadFromJsonAsync<NameLookupResult>(cancellationToken) ?? throw new UnreachableException();
    }
}
