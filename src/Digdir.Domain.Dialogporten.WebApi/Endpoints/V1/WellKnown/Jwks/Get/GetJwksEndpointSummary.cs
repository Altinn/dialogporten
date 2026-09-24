using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.WellKnown.Jwks.Get;

public sealed class GetJwksEndpointSummary : Summary<GetJwksEndpoint>
{
    public GetJwksEndpointSummary()
    {
        Summary = "Gets the JSON Web Key Set (JWKS) containing the public keys used to verify dialog token signatures";
        Description = """
                      This endpoint can be used by client integrations supporting automatic discovery of "OAuth 2.0 Authorization Server" metadata, enabling verification of dialog tokens issued by Dialogporten.

                      Dialogporten issues a single token type, the dialog token, signed with these keys and carrying the JOSE "typ" header "JWT". Besides the action grants in the "a" claim, the token's "e" claim lists the entities carrying an authorization context that the end user is authorized for, by entity id or by the "tokenRef" the service owner supplied on the context; a receiving service handling a request scoped to such an entity must check both that the entity is listed there and that "i" identifies the dialog being accessed. A tokenRef shared by multiple entities represents an OR-group: authorization for any group member adds the shared value.
                      """;
        Responses[StatusCodes.Status200OK] = "The OAuth 2.0 Authorization Server Metadata";
    }
}
