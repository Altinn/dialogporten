using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.WellKnown.Jwks.Get;

public sealed class GetJwksEndpointSummary : Summary<GetJwksEndpoint>
{
    public GetJwksEndpointSummary()
    {
        Summary = "Gets the JSON Web Key Set (JWKS) containing the public keys used to verify dialog token signatures";
        Description = """
                      This endpoint can be used by client integrations supporting automatic discovery of "OAuth 2.0 Authorization Server" metadata, enabling verification of dialog tokens issued by Dialogporten.
                      """;
        Responses[StatusCodes.Status200OK] = "The OAuth 2.0 Authorization Server Metadata";
    }
}
