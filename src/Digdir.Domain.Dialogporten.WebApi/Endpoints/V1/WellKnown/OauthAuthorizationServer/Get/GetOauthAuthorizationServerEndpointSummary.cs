using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.WellKnown.OauthAuthorizationServer.Get;

public sealed class GetOauthAuthorizationServerEndpointSummary : Summary<GetOauthAuthorizationServerEndpoint>
{
    public GetOauthAuthorizationServerEndpointSummary()
    {
        Summary = "Gets the OAuth 2.0 Metadata for automatic configuration of clients verifying dialog tokens";
        Description = """
                      This endpoint can be used by client integrations supporting automatic discovery of "OAuth 2.0 Authorization Server" metadata, enabling verification of dialog tokens issued by Dialogporten.
                      """;
        Responses[StatusCodes.Status200OK] = "The OAuth 2.0 Authorization Server Metadata";
    }
}
