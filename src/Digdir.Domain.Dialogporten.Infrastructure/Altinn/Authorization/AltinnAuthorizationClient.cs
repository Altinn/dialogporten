﻿using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Common.Exceptions;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
internal sealed class AltinnAuthorizationClient : IAltinnAuthorization
{
    private const string AuthorizeUrl = "authorization/api/v1/authorize";
    private const string AuthorizedPartiesUrl = "/accessmanagement/api/v1/resourceowner/authorizedparties?includeAltinn2=true";

    private readonly HttpClient _httpClient;
    private readonly IFusionCache _pdpCache;
    private readonly IFusionCache _partiesCache;
    private readonly IUser _user;
    private readonly ILogger _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public AltinnAuthorizationClient(
        HttpClient client,
        IFusionCacheProvider cacheProvider,
        IUser user,
        ILogger<AltinnAuthorizationClient> logger)
    {
        _httpClient = client ?? throw new ArgumentNullException(nameof(client));
        _pdpCache = cacheProvider.GetCache(nameof(Authorization)) ?? throw new ArgumentNullException(nameof(cacheProvider));
        _partiesCache = cacheProvider.GetCache(nameof(AuthorizedPartiesResult)) ?? throw new ArgumentNullException(nameof(cacheProvider));
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DialogDetailsAuthorizationResult> GetDialogDetailsAuthorization(
        DialogEntity dialogEntity,
        CancellationToken cancellationToken = default)
    {
        var request = new DialogDetailsAuthorizationRequest
        {
            Claims = _user.GetPrincipal().Claims.ToList(),
            ServiceResource = dialogEntity.ServiceResource,
            DialogId = dialogEntity.Id,
            Party = dialogEntity.Party,
            AltinnActions = dialogEntity.GetAltinnActions()
        };

        return await _pdpCache.GetOrSetAsync(request.GenerateCacheKey(), async token
            => await PerformDialogDetailsAuthorization(request, token), token: cancellationToken);
    }

    public async Task<DialogSearchAuthorizationResult> GetAuthorizedResourcesForSearch(
        List<string> constraintParties,
        List<string> serviceResources,
        CancellationToken cancellationToken = default)
    {
        var claims = _user.GetPrincipal().Claims.ToList();
        var request = new DialogSearchAuthorizationRequest
        {
            Claims = claims,
            ConstraintParties = constraintParties,
            ConstraintServiceResources = serviceResources
        };

        // We don't cache at this level, as the principal information is received from GetAuthorizedParties,
        // which is already cached
        return await PerformDialogSearchAuthorization(request, cancellationToken);
    }

    public async Task<AuthorizedPartiesResult> GetAuthorizedParties(IPartyIdentifier authenticatedParty, bool flatten = false,
        CancellationToken cancellationToken = default)
    {
        var authorizedPartiesRequest = new AuthorizedPartiesRequest(authenticatedParty);

        // Disabled until this bug is fixed: https://github.com/digdir/dialogporten/issues/1226
        // var authorizedParties = await _partiesCache.GetOrSetAsync(authorizedPartiesRequest.GenerateCacheKey(), async token
        // => await PerformAuthorizedPartiesRequest(authorizedPartiesRequest, token), token: cancellationToken);

        var authorizedParties = await PerformAuthorizedPartiesRequest(authorizedPartiesRequest, cancellationToken);

        return flatten ? GetFlattenedAuthorizedParties(authorizedParties) : authorizedParties;
    }

    public async Task<bool> HasListAuthorizationForDialog(DialogEntity dialog, CancellationToken cancellationToken)
    {
        var authorizedResourcesForSearch = await GetAuthorizedResourcesForSearch(
            [dialog.Party], [dialog.ServiceResource], cancellationToken);

        return authorizedResourcesForSearch.ResourcesByParties.Count > 0
               || authorizedResourcesForSearch.SubjectsByParties.Count > 0
               || authorizedResourcesForSearch.DialogIds.Contains(dialog.Id);
    }

    private static AuthorizedPartiesResult GetFlattenedAuthorizedParties(AuthorizedPartiesResult authorizedParties)
    {
        var flattenedAuthorizedParties = new AuthorizedPartiesResult();

        foreach (var authorizedParty in authorizedParties.AuthorizedParties)
        {
            Flatten(authorizedParty);
        }

        return flattenedAuthorizedParties;

        void Flatten(AuthorizedParty party, AuthorizedParty? parent = null)
        {
            if (party.SubParties is not null && party.SubParties.Count != 0)
            {
                foreach (var subParty in party.SubParties)
                {
                    Flatten(subParty, party);
                }
            }

            if (parent != null) party.ParentParty = parent.Party;
            party.SubParties = [];

            flattenedAuthorizedParties.AuthorizedParties.Add(party);
        }
    }

    private async Task<AuthorizedPartiesResult> PerformAuthorizedPartiesRequest(AuthorizedPartiesRequest authorizedPartiesRequest,
        CancellationToken token)
    {
        var authorizedPartiesDto = await SendAuthorizedPartiesRequest(authorizedPartiesRequest, token);
        if (authorizedPartiesDto is null || authorizedPartiesDto.Count == 0)
        {
            throw new UpstreamServiceException("access-management returned no authorized parties, missing Altinn profile?");
        }

        return AuthorizedPartiesHelper.CreateAuthorizedPartiesResult(authorizedPartiesDto, authorizedPartiesRequest);
    }

    private async Task<DialogSearchAuthorizationResult> PerformDialogSearchAuthorization(DialogSearchAuthorizationRequest request, CancellationToken token)
    {
        var partyIdentifier = request.Claims.GetEndUserPartyIdentifier() ?? throw new UnreachableException();
        var authorizedParties = await GetAuthorizedParties(partyIdentifier, flatten: true, cancellationToken: token);

        if (request.ConstraintParties.Count > 0)
        {
            authorizedParties.AuthorizedParties = authorizedParties.AuthorizedParties
                .Where(p => request.ConstraintParties.Contains(p.Party))
                .ToList();
        }

        var dialogSearchAuthorizationResult = new DialogSearchAuthorizationResult
        {
            ResourcesByParties = authorizedParties.AuthorizedParties
                .ToDictionary(
                    p => p.Party,
                    p => p.AuthorizedResources
                        .Where(r => request.ConstraintServiceResources.Count == 0 || request.ConstraintServiceResources.Contains(r))
                        .ToList())
                // Skip parties with no authorized resources
                .Where(kv => kv.Value.Count != 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value),

            SubjectsByParties = authorizedParties.AuthorizedParties
                .ToDictionary(
                    p => p.Party,
                    p => p.AuthorizedRoles)
                // Skip parties with no authorized roles
                .Where(kv => kv.Value.Count != 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        return dialogSearchAuthorizationResult;
    }

    private async Task<DialogDetailsAuthorizationResult> PerformDialogDetailsAuthorization(
        DialogDetailsAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var xacmlJsonRequest = DecisionRequestHelper.CreateDialogDetailsRequest(request);
        var xamlJsonResponse = await SendPdpRequest(xacmlJsonRequest, cancellationToken);
        LogIfIndeterminate(xamlJsonResponse, xacmlJsonRequest);

        return DecisionRequestHelper.CreateDialogDetailsResponse(request.AltinnActions, xamlJsonResponse);
    }

    private void LogIfIndeterminate(XacmlJsonResponse? response, XacmlJsonRequestRoot request)
    {
        if (response?.Response != null && response.Response.Any(result => result.Decision == "Indeterminate"))
        {
            DecisionRequestHelper.XacmlRequestRemoveSensitiveInfo(request.Request);

            _logger.LogError(
                "Authorization request to {Url} returned decision Indeterminate. Request: {@RequestJson}",
                AuthorizeUrl, request);
        }
    }

    private async Task<XacmlJsonResponse?> SendPdpRequest(
        XacmlJsonRequestRoot xacmlJsonRequest, CancellationToken cancellationToken) =>
        await SendRequest<XacmlJsonResponse>(
            AuthorizeUrl, xacmlJsonRequest, cancellationToken);

    private async Task<List<AuthorizedPartiesResultDto>?> SendAuthorizedPartiesRequest(
        AuthorizedPartiesRequest authorizedPartiesRequest, CancellationToken cancellationToken) =>
        await SendRequest<List<AuthorizedPartiesResultDto>>(
            AuthorizedPartiesUrl, authorizedPartiesRequest, cancellationToken);

    private async Task<T?> SendRequest<T>(string url, object request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Authorization request to {Url}: {RequestJson}", url, JsonSerializer.Serialize(request, SerializerOptions));
        return await _httpClient.PostAsJsonEnsuredAsync<T>(url, request, serializerOptions: SerializerOptions, cancellationToken: cancellationToken);
    }
}
