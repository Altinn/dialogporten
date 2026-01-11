using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using static Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common.TestTokenConstants;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;

public enum TokenKind
{
    EndUser,
    ServiceOwner
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

        _encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{settings.Value.TokenGeneratorUser}:{settings.Value.TokenGeneratorPassword}"));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await GetToken(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetToken(CancellationToken cancellationToken)
    {
        var overrides = _overridesAccessor.Current;

        var overrideToken = _kind switch
        {
            TokenKind.EndUser => overrides?.EndUser?.TokenOverride,
            TokenKind.ServiceOwner => overrides?.ServiceOwner?.TokenOverride,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(overrideToken))
        {
            return overrideToken;
        }

        var tokenEnvironment = Utils.GetTokenGeneratorEnvironment();
        var requestPath = _kind switch
        {
            TokenKind.EndUser => BuildEndUserRequestPath(overrides?.EndUser, tokenEnvironment),
            TokenKind.ServiceOwner => BuildServiceOwnerRequestPath(overrides?.ServiceOwner, tokenEnvironment),
            _ => throw new InvalidOperationException($"Unsupported token kind: {_kind}")
        };

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, $"{TestTokenBaseUrl}{requestPath}");
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", _encodedCredentials);

        var httpClient = _httpClientFactory.CreateClient("TokenGenerator");
        using var tokenResult = await httpClient.SendAsync(tokenRequest, cancellationToken);
        tokenResult.EnsureSuccessStatusCode();

        return await tokenResult.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string BuildEndUserRequestPath(
        EndUserTokenOverrides? overrides,
        string tokenEnvironment)
    {
        var scopes = overrides?.Scopes ?? EndUserScopes;
        var ssn = overrides?.Ssn ?? DefaultEndUserSsn;

        return
            "/GetPersonalToken" +
            $"?env={tokenEnvironment}" +
            $"&scopes={Uri.EscapeDataString(scopes)}" +
            $"&pid={Uri.EscapeDataString(ssn)}" +
            $"&ttl={DefaultTokenTtl}";
    }

    private static string BuildServiceOwnerRequestPath(
        ServiceOwnerTokenOverrides? overrides,
        string tokenEnvironment)
    {
        var scopes = overrides?.Scopes ?? ServiceOwnerScopes;
        var orgName = overrides?.OrgName ?? DefaultServiceOwnerOrgName;
        var orgNumber = overrides?.OrgNumber ?? GetDefaultServiceOwnerOrgNumber(tokenEnvironment);

        return
            "/GetEnterpriseToken" +
            $"?env={tokenEnvironment}" +
            $"&scopes={Uri.EscapeDataString(scopes)}" +
            $"&org={Uri.EscapeDataString(orgName)}" +
            $"&orgNo={Uri.EscapeDataString(orgNumber)}" +
            $"&ttl={DefaultTokenTtl}";
    }

    private static string GetDefaultServiceOwnerOrgNumber(string tokenEnvironment) =>
        tokenEnvironment == "yt01"
            ? Yt01ServiceOwnerOrgNr
            : DefaultServiceOwnerOrgNr;
}
