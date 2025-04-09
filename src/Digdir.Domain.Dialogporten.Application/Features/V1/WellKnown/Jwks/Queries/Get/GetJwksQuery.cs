using Mediator;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.WellKnown.Jwks.Queries.Get;

public sealed class GetJwksQuery : IRequest<GetJwksDto>;

internal sealed class GetJwksQueryHandler : IRequestHandler<GetJwksQuery, GetJwksDto>
{
    private readonly ApplicationSettings _applicationSettings;

    public GetJwksQueryHandler(
        IOptions<ApplicationSettings> applicationSettings)
    {
        _applicationSettings = applicationSettings.Value;
    }

    public async ValueTask<GetJwksDto> Handle(GetJwksQuery request, CancellationToken cancellationToken)
    {
        return await Task.FromResult(new GetJwksDto
        {
            Keys = {
                new Jwk
                {
                    X = _applicationSettings.Dialogporten.Ed25519KeyPairs.Primary.PublicComponent,
                    Kid = _applicationSettings.Dialogporten.Ed25519KeyPairs.Primary.Kid
                },
                new Jwk
                {
                    X = _applicationSettings.Dialogporten.Ed25519KeyPairs.Secondary.PublicComponent,
                    Kid = _applicationSettings.Dialogporten.Ed25519KeyPairs.Secondary.Kid
                }
            }
        });
    }
}
