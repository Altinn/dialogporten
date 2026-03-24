using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace Digdir.Library.Dialogporten.E2E.Common;

public enum TokenKind
{
    EndUser,
    ServiceOwner,
    SystemUser
}

public sealed class TestTokenHandler : DelegatingHandler
{
    private readonly TokenKind _kind;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenOverridesAccessor _overridesAccessor;
    private readonly string _encodedCredentials;

    public TestTokenHandler(
        TokenKind kind,
        IOptions<E2ESettings> settings,
        IHttpClientFactory httpClientFactory,
        ITokenOverridesAccessor overridesAccessor)
    {
        _kind = kind;
        _httpClientFactory = httpClientFactory;
        _overridesAccessor = overridesAccessor;
        _encodedCredentials = TestTokenGenerator.BuildEncodedCredentials(
            settings.Value.TokenGeneratorUser,
            settings.Value.TokenGeneratorPassword);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await GetToken(cancellationToken);

        if (token.Length == 0)
        {
            request.Headers.Authorization = null;
            return await base.SendAsync(request, cancellationToken);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetToken(CancellationToken cancellationToken)
    {
        var overrides = _overridesAccessor.Current;
        var isEndUserHandler = _kind == TokenKind.EndUser;
        var tokenKind = isEndUserHandler && overrides?.EndUserType == EndUserTokenType.SystemUser
            ? TokenKind.SystemUser
            : _kind;

        var overrideToken = tokenKind switch
        {
            TokenKind.EndUser => overrides?.EndUser?.TokenOverride,
            TokenKind.ServiceOwner => overrides?.ServiceOwner?.TokenOverride,
            TokenKind.SystemUser when isEndUserHandler => overrides?.SystemUser?.TokenOverride,
            _ => null
        };

        if (overrideToken is not null)
        {
            return overrideToken;
        }

        return await TestTokenGenerator.GenerateTokenAsync(
            kind: tokenKind,
            encodedCredentials: _encodedCredentials,
            createHttpClient: () => _httpClientFactory.CreateClient("TokenGenerator"),
            endUserOverrides: overrides?.EndUser,
            serviceOwnerOverrides: overrides?.ServiceOwner,
            systemUserOverrides: isEndUserHandler ? overrides?.SystemUser : null,
            cancellationToken);
    }
}
