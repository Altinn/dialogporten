using System.Net.Http.Headers;
using System.Text;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Microsoft.Extensions.Options;
using static Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common.TestTokenConstants;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;

public class EndUserTokenHandler : DelegatingHandler
{
    private readonly HttpClient _httpClient;
    private readonly string _encodedCredentials;

    public EndUserTokenHandler(IOptions<E2ESettings> settings, HttpClient httpClient)
    {
        _httpClient = httpClient;

        _encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{settings.Value.TokenGeneratorUser}:{settings.Value.TokenGeneratorPassword}"));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await GetEndUserToken(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetEndUserToken(CancellationToken cancellationToken)
    {
        var tokenEnvironment = Utils.GetTokenGeneratorEnvironment();

        var requestPath =
            "/GetPersonalToken" +
            $"?env={tokenEnvironment}" +
            $"&scopes={Uri.EscapeDataString(AuthorizationScope.EndUser)}" +
            $"&pid={Uri.EscapeDataString(DefaultEndUserSsn)}" +
            $"&ttl={DefaultTokenTtl}";

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, $"{TestTokenBaseUrl}{requestPath}");
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", _encodedCredentials);

        using var tokenResult = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        tokenResult.EnsureSuccessStatusCode();

        return await tokenResult.Content.ReadAsStringAsync(cancellationToken);
    }


}
