using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.WellKnown.Jwks.Get;

public sealed class GetJwksEndpointSummary : Summary<GetJwksEndpoint>
{
    public GetJwksEndpointSummary()
    {
        Summary = "Gets the JSON Web Key Set (JWKS) containing the public keys used to verify dialog token signatures";
        Description = """
                      This endpoint can be used by client integrations supporting automatic discovery of "OAuth 2.0 Authorization Server" metadata, enabling verification of dialog tokens issued by Dialogporten.

                      Dialog tokens carry the JOSE "typ" header "JWT". Receiving services must validate the "typ" header and reject a token whose type they do not expect: other token types signed with these keys carry an explicit type per RFC 8725.
                      """;
        Responses[StatusCodes.Status200OK] = "The OAuth 2.0 Authorization Server Metadata";
    }
}
