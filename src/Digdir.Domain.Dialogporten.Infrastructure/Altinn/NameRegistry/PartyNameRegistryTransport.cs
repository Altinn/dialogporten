using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.Domain.Common;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.NameRegistry;

internal interface IPartyNameRegistryTransport
{
    Task<HttpResponseMessage> QueryPartyName(
        NameLookup nameLookup,
        CancellationToken cancellationToken
    );

    sealed class NameLookup
    {
        public List<string> Data { get; set; } = null!;
    }

    sealed class NameLookupResult
    {
        public List<NameLookupEntry> Data { get; set; } = null!;
    }

    sealed class NameLookupEntry
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

    public async Task<HttpResponseMessage> QueryPartyName(
        IPartyNameRegistryTransport.NameLookup nameLookup,
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
    public Task<HttpResponseMessage> QueryPartyName(
        IPartyNameRegistryTransport.NameLookup nameLookup,
        CancellationToken cancellationToken
    )
    {
        var name = nameLookup switch
        {
            var x when x.Data
                .Single()
                .StartsWith(Constants.SystemuserPrefix, StringComparison.InvariantCulture) => "Systembruker",
            _ => "Brando Sando"
        };

        return Task.FromResult(new HttpResponseMessage
        {
            Content = JsonContent.Create(new IPartyNameRegistryTransport.NameLookupResult
            {
                Data =
                [
                    new IPartyNameRegistryTransport.NameLookupEntry
                    {
                        DisplayName = name
                    }
                ]
            }),
            StatusCode = HttpStatusCode.OK
        });
    }
}
