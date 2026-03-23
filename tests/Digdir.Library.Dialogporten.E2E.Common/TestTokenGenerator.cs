using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Digdir.Library.Dialogporten.E2E.Common.E2EConstants;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class TestTokenGenerator
{
    private static readonly ConcurrentDictionary<string, string> TokenCache = new();
    private static readonly HttpClient SharedHttpClient = new();

    public static Task<string> GenerateTokenAsync(
        TokenKind kind,
        E2ESettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return GenerateTokenAsync(
            requestPath: BuildRequestPath(kind),
            encodedCredentials: BuildEncodedCredentials(settings.TokenGeneratorUser, settings.TokenGeneratorPassword),
            createHttpClient: static () => SharedHttpClient,
            cancellationToken);
    }

    internal static Task<string> GenerateTokenAsync(
        TokenKind kind,
        string encodedCredentials,
        Func<HttpClient> createHttpClient,
        EndUserTokenOverrides? endUserOverrides,
        ServiceOwnerTokenOverrides? serviceOwnerOverrides,
        SystemUserTokenOverrides? systemUserOverrides,
        CancellationToken cancellationToken) =>
        GenerateTokenAsync(
            requestPath: BuildRequestPath(kind, endUserOverrides, serviceOwnerOverrides, systemUserOverrides),
            encodedCredentials,
            createHttpClient,
            cancellationToken);

    internal static string BuildEncodedCredentials(string user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(password);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));
    }

    private static async Task<string> GenerateTokenAsync(
        string requestPath,
        string encodedCredentials,
        Func<HttpClient> createHttpClient,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(encodedCredentials);
        ArgumentNullException.ThrowIfNull(createHttpClient);

        if (TokenCache.TryGetValue(requestPath, out var cachedToken))
        {
            return cachedToken;
        }

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, $"{TestTokenBaseUrl}{requestPath}");
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);

        var httpClient = createHttpClient();
        using var tokenResult = await httpClient.SendAsync(tokenRequest, cancellationToken);
        tokenResult.EnsureSuccessStatusCode();

        var token = await tokenResult.Content.ReadAsStringAsync(cancellationToken);
        TokenCache[requestPath] = token;
        return token;
    }

    private static string BuildRequestPath(
        TokenKind kind,
        EndUserTokenOverrides? endUserOverrides = null,
        ServiceOwnerTokenOverrides? serviceOwnerOverrides = null,
        SystemUserTokenOverrides? systemUserOverrides = null)
    {
        var tokenEnvironment = Environment.GetTokenGeneratorEnvironment();

        if (kind == TokenKind.EndUser)
        {
            return BuildEndUserRequestPath(endUserOverrides, tokenEnvironment);
        }

        if (kind == TokenKind.ServiceOwner)
        {
            return BuildServiceOwnerRequestPath(serviceOwnerOverrides, tokenEnvironment);
        }

        if (kind == TokenKind.SystemUser)
        {
            return BuildSystemUserRequestPath(systemUserOverrides, tokenEnvironment);
        }

        throw new InvalidOperationException($"Unsupported token kind: {kind}");
    }

    private static string BuildEndUserRequestPath(
        EndUserTokenOverrides? overrides,
        string tokenEnvironment) =>
        "/GetPersonalToken" +
        $"?env={tokenEnvironment}" +
        $"&scopes={Uri.EscapeDataString(overrides?.Scopes ?? EndUserScopes)}" +
        $"&pid={Uri.EscapeDataString(overrides?.Ssn ?? DefaultEndUserSsn)}" +
        $"&ttl={DefaultTokenTtl}";

    private static string BuildServiceOwnerRequestPath(
        ServiceOwnerTokenOverrides? overrides,
        string tokenEnvironment) =>
        "/GetEnterpriseToken" +
        $"?env={tokenEnvironment}" +
        $"&scopes={Uri.EscapeDataString(overrides?.Scopes ?? ServiceOwnerScopes)}" +
        $"&org={Uri.EscapeDataString(overrides?.OrgName ?? DefaultServiceOwnerOrgName)}" +
        $"&orgNo={Uri.EscapeDataString(overrides?.OrgNumber ?? GetDefaultServiceOwnerOrgNr())}" +
        $"&ttl={DefaultTokenTtl}";

    private static string BuildSystemUserRequestPath(
        SystemUserTokenOverrides? overrides,
        string tokenEnvironment) =>
        "/GetSystemUserToken" +
        $"?env={tokenEnvironment}" +
        $"&scopes={Uri.EscapeDataString(overrides?.Scopes ?? SystemUserScopes)}" +
        $"&systemUserId={Uri.EscapeDataString(overrides?.SystemUserId ?? DefaultSystemUserId)}" +
        $"&systemUserOrg={Uri.EscapeDataString(overrides?.SystemUserOrg ?? DefaultSystemUserOrgNo)}" +
        $"&ttl={DefaultTokenTtl}";
}
