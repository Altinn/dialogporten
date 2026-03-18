using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Microsoft.Extensions.Options;
using static Digdir.Library.Dialogporten.E2E.Common.E2EConstants;

namespace Digdir.Library.Dialogporten.E2E.Common;

public enum TokenKind
{
    EndUser,
    ServiceOwner,
    SystemUser
}

public sealed class TestTokenHandler : DelegatingHandler
{
    private static readonly ConcurrentDictionary<string, string> TokenCache = new();
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
        var tokenKind = overrides?.EndUserType == EndUserTokenType.SystemUser
            ? TokenKind.SystemUser
            : _kind;

        var overrideToken = tokenKind switch
        {
            TokenKind.EndUser => overrides?.EndUser?.TokenOverride,
            TokenKind.ServiceOwner => overrides?.ServiceOwner?.TokenOverride,
            TokenKind.SystemUser => overrides?.SystemUser?.TokenOverride,
            _ => null
        };

        if (overrideToken is not null)
        {
            return overrideToken;
        }

        var tokenEnvironment = Environment.GetTokenGeneratorEnvironment();
        var requestPath = tokenKind switch
        {
            TokenKind.EndUser => BuildEndUserRequestPath(overrides?.EndUser, tokenEnvironment),
            TokenKind.ServiceOwner => BuildServiceOwnerRequestPath(overrides?.ServiceOwner, tokenEnvironment),
            TokenKind.SystemUser => BuildSystemUserRequestPath(overrides?.SystemUser, tokenEnvironment),
            _ => throw new InvalidOperationException($"Unsupported token kind: {tokenKind}")
        };

        if (TokenCache.TryGetValue(requestPath, out var cachedToken))
        {
            return cachedToken;
        }

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, $"{TestTokenBaseUrl}{requestPath}");
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", _encodedCredentials);

        var httpClient = _httpClientFactory.CreateClient("TokenGenerator");
        using var tokenResult = await httpClient.SendAsync(tokenRequest, cancellationToken);
        tokenResult.EnsureSuccessStatusCode();

        var token = await tokenResult.Content.ReadAsStringAsync(cancellationToken);
        TokenCache[requestPath] = token;
        return token;
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

    private static string BuildSystemUserRequestPath(
        SystemUserTokenOverrides? overrides,
        string tokenEnvironment)
    {
        var scopes = overrides?.Scopes ?? SystemUserScopes;
        var systemUserId = overrides?.SystemUserId ?? DefaultSystemUserId;
        var systemUserOrg = overrides?.SystemUserOrg ?? DefaultSystemUserOrgNo;

        return
            "/GetSystemUserToken" +
            $"?env={tokenEnvironment}" +
            $"&scopes={Uri.EscapeDataString(scopes)}" +
            $"&systemUserId={Uri.EscapeDataString(systemUserId)}" +
            $"&systemUserOrg={Uri.EscapeDataString(systemUserOrg)}";
    }

    private static string GetDefaultServiceOwnerOrgNumber(string tokenEnvironment) =>
        tokenEnvironment == "yt01"
            ? Yt01ServiceOwnerOrgNr
            : DefaultServiceOwnerOrgNr;
}
