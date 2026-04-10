using Altinn.ApiClients.Maskinporten.Config;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Infrastructure;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Authentication;

namespace Digdir.Domain.Dialogporten.WebApi;

internal static class OpenApiDocumentGenerationOverrides
{
    internal static Dictionary<string, string?> GetOpenApiDocumentGenerationOverrides()
    {
        const string altinnExampleUri = "https://altinn.example/";
        const string ed25519PrivateComponent = "ns9Mgams90E5bCNGg9iSXONvRvASFcWF_Nb_JJ8oAEA";
        const string ed25519PublicComponent = "qIn67qFQUBiwW2kv7J-5CdUCdR67CzOSnwXPBunh0d0";
        const string encodedJwk = "eyJrdHkiOiJPS1AiLCJjcnYiOiJFZDI1NTE5Iiwia2lkIjoib3BlbmFwaS1kb2NnZW4tbWFza2lucG9ydGVuIiwieCI6InFJbjY3cUZRVUJpd1cya3Y3Si01Q2RVQ2RSNjdDek9TbndYUEJ1bmgwZDAiLCJkIjoibnM5TWdhbXM5MEU1YkNOR2c5aVNYT052UnZBU0ZjV0ZfTmJfSko4b0FFQSJ9";
        const string infrastructure = InfrastructureSettings.ConfigurationSectionName;
        const string application = ApplicationSettings.ConfigurationSectionName;
        const string webApi = WebApiSettings.SectionName;
        const string dialogporten = nameof(ApplicationSettings.Dialogporten);
        const string ed25519KeyPairs = nameof(DialogportenSettings.Ed25519KeyPairs);
        const string authentication = nameof(WebApiSettings.Authentication);
        const string jwtBearerTokenSchemas = nameof(AuthenticationOptions.JwtBearerTokenSchemas);
        const string infrastructureAltinn = $"{infrastructure}:{nameof(InfrastructureSettings.Altinn)}";
        const string infrastructureMaskinporten = $"{infrastructure}:{nameof(InfrastructureSettings.Maskinporten)}";
        const string applicationDialogporten = $"{application}:{dialogporten}";
        const string applicationDialogportenEd25519KeyPairs = $"{applicationDialogporten}:{ed25519KeyPairs}";
        const string primaryEd25519KeyPair = $"{applicationDialogportenEd25519KeyPairs}:{nameof(Ed25519KeyPairs.Primary)}";
        const string secondaryEd25519KeyPair = $"{applicationDialogportenEd25519KeyPairs}:{nameof(Ed25519KeyPairs.Secondary)}";
        const string webApiJwtBearerTokenSchemas = $"{webApi}:{authentication}:{jwtBearerTokenSchemas}";

        return new Dictionary<string, string?>
        {
            [$"{infrastructure}:{nameof(InfrastructureSettings.DialogDbConnectionString)}"] =
                "Host=localhost;Port=5432;Database=dialogporten;Username=postgres;Password=postgres;Timeout=1;Command Timeout=1;Pooling=false",
            [$"{infrastructure}:{nameof(InfrastructureSettings.Redis)}:{nameof(RedisSettings.ConnectionString)}"] =
                "localhost:6379,abortConnect=false,connectTimeout=1000",
            [$"{infrastructureAltinn}:{nameof(AltinnPlatformSettings.BaseUri)}"] = altinnExampleUri,
            [$"{infrastructureAltinn}:{nameof(AltinnPlatformSettings.EventsBaseUri)}"] = altinnExampleUri,
            [$"{infrastructureAltinn}:{nameof(AltinnPlatformSettings.SubscriptionKey)}"] = "openapi-docgen",
            [$"{infrastructure}:{nameof(InfrastructureSettings.AltinnCdn)}:{nameof(AltinnCdnPlatformSettings.BaseUri)}"] = altinnExampleUri,
            [$"{infrastructureMaskinporten}:{nameof(MaskinportenSettings.Environment)}"] = "test",
            [$"{infrastructureMaskinporten}:{nameof(MaskinportenSettings.ClientId)}"] = "openapi-docgen",
            [$"{infrastructureMaskinporten}:{nameof(MaskinportenSettings.Scope)}"] = "altinn:events.publish",
            [$"{infrastructureMaskinporten}:{nameof(MaskinportenSettings.EncodedJwk)}"] = encodedJwk,
            [$"{infrastructureMaskinporten}:{nameof(MaskinportenSettings.TokenExchangeEnvironment)}"] = "at23",
            [$"{infrastructure}:{nameof(InfrastructureSettings.MassTransit)}:{nameof(MassTransitSettings.Host)}"] =
                "Endpoint=sb://localhost/;SharedAccessKeyName=openapi-docgen;SharedAccessKey=openapi-docgen",
            [$"{applicationDialogporten}:{nameof(DialogportenSettings.BaseUri)}"] = altinnExampleUri,
            [$"{primaryEd25519KeyPair}:{nameof(Ed25519KeyPair.Kid)}"] = "openapi-docgen-primary",
            [$"{primaryEd25519KeyPair}:{nameof(Ed25519KeyPair.PrivateComponent)}"] = ed25519PrivateComponent,
            [$"{primaryEd25519KeyPair}:{nameof(Ed25519KeyPair.PublicComponent)}"] = ed25519PublicComponent,
            [$"{secondaryEd25519KeyPair}:{nameof(Ed25519KeyPair.Kid)}"] = "openapi-docgen-secondary",
            [$"{secondaryEd25519KeyPair}:{nameof(Ed25519KeyPair.PrivateComponent)}"] = ed25519PrivateComponent,
            [$"{secondaryEd25519KeyPair}:{nameof(Ed25519KeyPair.PublicComponent)}"] = ed25519PublicComponent,
            [$"{webApiJwtBearerTokenSchemas}:0:{nameof(JwtBearerTokenSchemasOptions.Name)}"] = "Maskinporten",
            [$"{webApiJwtBearerTokenSchemas}:0:{nameof(JwtBearerTokenSchemasOptions.WellKnown)}"] = altinnExampleUri,
            [$"{webApiJwtBearerTokenSchemas}:1:{nameof(JwtBearerTokenSchemasOptions.Name)}"] = "Altinn",
            [$"{webApiJwtBearerTokenSchemas}:1:{nameof(JwtBearerTokenSchemasOptions.WellKnown)}"] = altinnExampleUri,
            [$"{webApiJwtBearerTokenSchemas}:2:{nameof(JwtBearerTokenSchemasOptions.Name)}"] = "Idporten",
            [$"{webApiJwtBearerTokenSchemas}:2:{nameof(JwtBearerTokenSchemasOptions.WellKnown)}"] = altinnExampleUri
        };
    }
}
