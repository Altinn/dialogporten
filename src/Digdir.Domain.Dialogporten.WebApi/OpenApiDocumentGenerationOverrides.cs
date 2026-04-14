using System.Text;
using System.Text.Json;

namespace Digdir.Domain.Dialogporten.WebApi;

internal static class OpenApiDocumentGenerationOverrides
{
    internal static Stream CreateOverridesJson()
    {
        const string altinnExampleUri = "https://altinn.example/";
        const string ed25519PrivateComponent = "ns9Mgams90E5bCNGg9iSXONvRvASFcWF_Nb_JJ8oAEA";
        const string ed25519PublicComponent = "qIn67qFQUBiwW2kv7J-5CdUCdR67CzOSnwXPBunh0d0";
        const string encodedJwk = "eyJrdHkiOiJPS1AiLCJjcnYiOiJFZDI1NTE5Iiwia2lkIjoib3BlbmFwaS1kb2NnZW4tbWFza2lucG9ydGVuIiwieCI6InFJbjY3cUZRVUJpd1cya3Y3Si01Q2RVQ2RSNjdDek9TbndYUEJ1bmgwZDAiLCJkIjoibnM5TWdhbXM5MEU1YkNOR2c5aVNYT052UnZBU0ZjV0ZfTmJfSko4b0FFQSJ9";
        var payload = new
        {
            Infrastructure = new
            {
                DialogDbConnectionString =
                    "Host=localhost;Port=5432;Database=dialogporten;Username=postgres;Password=postgres;Timeout=1;Command Timeout=1;Pooling=false",
                Redis = new
                {
                    ConnectionString = "localhost:6379,abortConnect=false,connectTimeout=1000"
                },
                Altinn = new
                {
                    BaseUri = altinnExampleUri,
                    EventsBaseUri = altinnExampleUri,
                    SubscriptionKey = "openapi-docgen"
                },
                AltinnCdn = new
                {
                    BaseUri = altinnExampleUri
                },
                Maskinporten = new
                {
                    Environment = "test",
                    ClientId = "openapi-docgen",
                    Scope = "altinn:events.publish",
                    EncodedJwk = encodedJwk,
                    TokenExchangeEnvironment = "at23"
                },
                MassTransit = new
                {
                    Host = "Endpoint=sb://localhost/;SharedAccessKeyName=openapi-docgen;SharedAccessKey=openapi-docgen"
                }
            },
            Application = new
            {
                Dialogporten = new
                {
                    BaseUri = altinnExampleUri,
                    Ed25519KeyPairs = new
                    {
                        Primary = new
                        {
                            Kid = "openapi-docgen-primary",
                            PrivateComponent = ed25519PrivateComponent,
                            PublicComponent = ed25519PublicComponent
                        },
                        Secondary = new
                        {
                            Kid = "openapi-docgen-secondary",
                            PrivateComponent = ed25519PrivateComponent,
                            PublicComponent = ed25519PublicComponent
                        }
                    }
                }
            },
            WebApi = new
            {
                Authentication = new
                {
                    JwtBearerTokenSchemas = new[]
                    {
                        new { Name = "Maskinporten", WellKnown = altinnExampleUri },
                        new { Name = "Altinn", WellKnown = altinnExampleUri },
                        new { Name = "Idporten", WellKnown = altinnExampleUri }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }
}
