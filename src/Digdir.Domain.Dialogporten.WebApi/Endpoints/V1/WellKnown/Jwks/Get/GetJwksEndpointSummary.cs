using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.WellKnown.Jwks.Get;

public sealed class GetJwksEndpointSummary : Summary<GetJwksEndpoint>
{
    public GetJwksEndpointSummary()
    {
        Summary = "Gets the JSON Web Key Set (JWKS) containing the public keys used to verify dialog token signatures";
        Description = """
                      This endpoint can be used by client integrations supporting automatic discovery of "OAuth 2.0 Authorization Server" metadata, enabling verification of dialog tokens issued by Dialogporten.

                      Dialogporten issues two token types, both signed with these keys and distinguished by the JOSE "typ" header: "dialogtoken+jwt" (the dialog token) and "dialogcontexttoken+jwt" (a narrower token scoped to a single authorization-context-carrying entity). Receiving services must validate the "typ" header and reject a token whose type they do not expect.
                      """;
        Responses[StatusCodes.Status200OK] = "The OAuth 2.0 Authorization Server Metadata";
    }
}
