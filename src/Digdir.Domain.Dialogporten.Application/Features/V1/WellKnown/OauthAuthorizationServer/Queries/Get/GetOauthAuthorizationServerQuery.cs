using Mediator;
using Microsoft.Extensions.Options;
using Constants = Digdir.Domain.Dialogporten.Application.Features.V1.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.WellKnown.OauthAuthorizationServer.Queries.Get;

public sealed class GetOauthAuthorizationServerQuery : IRequest<GetOauthAuthorizationServerDto>;

internal sealed class GetOauthAuthorizationServerQueryHandler : IRequestHandler<GetOauthAuthorizationServerQuery, GetOauthAuthorizationServerDto>
{
    private readonly ApplicationSettings _applicationSettings;

    public GetOauthAuthorizationServerQueryHandler(
        IOptions<ApplicationSettings> applicationSettings)
    {
        _applicationSettings = applicationSettings.Value;
    }

    public async ValueTask<GetOauthAuthorizationServerDto> Handle(GetOauthAuthorizationServerQuery request, CancellationToken cancellationToken)
    {
        var issuerUrl = _applicationSettings.Dialogporten.BaseUri.AbsoluteUri.TrimEnd('/') + Constants.DialogTokenIssuerVersion;
        return await Task.FromResult(new GetOauthAuthorizationServerDto
        {
            Issuer = issuerUrl,
            JwksUri = $"{issuerUrl}/.well-known/jwks.json"
        });
    }
}
