using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.WellKnown.OauthAuthorizationServer.Get;

public sealed class GetOauthAuthorizationServerEndpointSummary : Summary<GetOauthAuthorizationServerEndpoint>
{
    public GetOauthAuthorizationServerEndpointSummary()
    {
        Summary = "Gets the OAuth 2.0 Metadata for automatic configuration of clients verifying dialog tokens";
        Description = """
                      This endpoint can be used by client integrations supporting automatic discovery of "OAuth 2.0 Authorization Server" metadata, enabling verification of dialog tokens issued by Dialogporten.

                      Dialogporten issues a single token type, the dialog token, signed with the published keys and carrying the JOSE "typ" header "JWT". Besides the action grants in the "a" claim, the token's "e" claim lists the entities carrying an authorization context that the end user is authorized for, by entity id or by the "tokenRef" the service owner supplied on the context; a receiving service handling a request scoped to such an entity must check that the entity is listed there.
                      """;
        Responses[StatusCodes.Status200OK] = "The OAuth 2.0 Authorization Server Metadata";
    }
}
