using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using static Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common.TestTokenConstants;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;

public class ServiceOwnerTokenHandler : DelegatingHandler
{
    private readonly HttpClient _httpClient;
    private readonly string _encodedCredentials;

    public ServiceOwnerTokenHandler(IOptions<E2ESettings> settings, HttpClient httpClient)
    {
        _httpClient = httpClient;

        _encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{settings.Value.TokenGeneratorUser}:{settings.Value.TokenGeneratorPassword}"));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await GetServiceOwnerToken(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetServiceOwnerToken(CancellationToken cancellationToken)
    {
        var tokenEnvironment = Utils.GetTokenGeneratorEnvironment();

        var orgNumber = tokenEnvironment == "yt01"
            ? Yt01ServiceOwnerOrgNr
            : DefaultServiceOwnerOrgNr;

        var requestPath =
            $"/GetEnterpriseToken" +
            $"?env={tokenEnvironment}" +
            $"&scopes={Uri.EscapeDataString(ServiceOwnerScopes)}" +
            $"&org={Uri.EscapeDataString(DefaultServiceOwnerOrgName)}" +
            $"&orgNo={Uri.EscapeDataString(orgNumber)}" +
            $"&ttl={DefaultTokenTtl}";

        var tokenRequest = new HttpRequestMessage(HttpMethod.Get, $"{TestTokenBaseUrl}{requestPath}");
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", _encodedCredentials);

        var tokenResult = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        tokenResult.EnsureSuccessStatusCode();

        return await tokenResult.Content.ReadAsStringAsync(cancellationToken);
    }
}
