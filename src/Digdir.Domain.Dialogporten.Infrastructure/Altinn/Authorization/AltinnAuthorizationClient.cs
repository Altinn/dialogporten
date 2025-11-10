using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Infrastructure.Common.Exceptions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IFusionCache _subjectResourcesCache;
    private readonly IUser _user;
    private readonly IDialogDbContext _db;
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public AltinnAuthorizationClient(
        HttpClient client,
        IFusionCacheProvider cacheProvider,
        IUser user,
        IDialogDbContext db,
        ILogger<AltinnAuthorizationClient> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _httpClient = client ?? throw new ArgumentNullException(nameof(client));
        _pdpCache = cacheProvider.GetCache(nameof(Authorization)) ?? throw new ArgumentNullException(nameof(cacheProvider));
        _partiesCache = cacheProvider.GetCache(nameof(AuthorizedPartiesResult)) ?? throw new ArgumentNullException(nameof(cacheProvider));
        _subjectResourcesCache = cacheProvider.GetCache(nameof(SubjectResource)) ?? throw new ArgumentNullException(nameof(cacheProvider));
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    }

    public async Task<DialogDetailsAuthorizationResult> GetDialogDetailsAuthorization(
        DialogEntity dialogEntity,
        CancellationToken cancellationToken = default)
    {
        var request = new DialogDetailsAuthorizationRequest
        {
            ClaimsPrincipal = _user.GetPrincipal(),
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

        if (authenticatedParty
            is IdportenSelfIdentifiedUserIdentifier
            or AltinnSelfIdentifiedUserIdentifier
            or FeideUserIdentifier)
        {
            // Self-identified/Feide users are not currently supported in the access management API, so we need to emulate it.
            // Assume they can only represent themselves, and have no delegated rights.

            // We currently do not have any support in the Register API to resolve the name of self-identified users,
            // so we need to get this from the request context, which means this will ONLY work in end-user contexts
            // where there is a end-user token available, ie. not in service owner contexts (using ?EndUserId=...) or
            // service contexts

            var authorizedPartiesResultDto = new AuthorizedPartiesResultDto
            {
                PartyUuid = _user.GetPrincipal().TryGetPartyUuid(out var uuid) ? uuid : Guid.Empty,
                PartyId = _user.GetPrincipal().TryGetPartyId(out var partyId) ? partyId : 0,
                Name = authorizedPartiesRequest.Value,
                OrganizationNumber = "",
                Type = AuthorizedPartiesHelper.PartyTypeSelfIdentified,
                IsDeleted = false,
                AuthorizedRoles = [AuthorizedPartiesHelper.SelfIdentifiedUserRoleCode],
                OnlyHierarchyElementWithNoAccess = false,
                AuthorizedResources = [],
                AuthorizedAccessPackages = [],
                AuthorizedInstances = [],
                Subunits = []
            };

            return AuthorizedPartiesHelper.CreateAuthorizedPartiesResult([authorizedPartiesResultDto], authorizedPartiesRequest);
        }

        AuthorizedPartiesResult authorizedParties;
        using (_logger.TimeOperation(nameof(GetAuthorizedParties)))
        {
            authorizedParties = await _partiesCache.GetOrSetAsync(authorizedPartiesRequest.GenerateCacheKey(), async token
                => await PerformAuthorizedPartiesRequest(authorizedPartiesRequest, token), token: cancellationToken);
        }

        return flatten ? GetFlattenedAuthorizedParties(authorizedParties) : authorizedParties;
    }

    public async Task<bool> HasListAuthorizationForDialog(DialogEntity dialog, CancellationToken cancellationToken)
    {
        var authorizedResourcesForSearch = await GetAuthorizedResourcesForSearch(
            [dialog.Party], [dialog.ServiceResource], cancellationToken);

        return authorizedResourcesForSearch.ResourcesByParties.Count > 0
               || authorizedResourcesForSearch.DialogIds.Contains(dialog.Id);
    }

    public bool UserHasRequiredAuthLevel(int minimumAuthenticationLevel) =>
        minimumAuthenticationLevel <= _user.GetPrincipal().GetAuthenticationLevel();

    public async Task<bool> UserHasRequiredAuthLevel(string serviceResource, CancellationToken cancellationToken)
    {
        var minimumAuthenticationLevel = await _db.ResourcePolicyInformation
            .Where(x => x.Resource == serviceResource)
            .Select(x => x.MinimumAuthenticationLevel)
            .FirstOrDefaultAsync(cancellationToken);

        return UserHasRequiredAuthLevel(minimumAuthenticationLevel);
    }

    // Create static empty lists to reuse and avoid allocations
    private static readonly List<string> EmptyStringList = [];
    private static readonly List<AuthorizedParty> EmptySubPartiesList = [];

    private static AuthorizedPartiesResult GetFlattenedAuthorizedParties(AuthorizedPartiesResult authorizedParties)
    {
        var topLevelCount = authorizedParties.AuthorizedParties.Count;

        var totalCapacity = topLevelCount;
        for (var i = 0; i < topLevelCount; i++)
        {
            var party = authorizedParties.AuthorizedParties[i];
            if (party.SubParties != null)
            {
                totalCapacity += party.SubParties.Count;
            }
        }

        // Preallocate the exact size needed
        var flattenedList = new List<AuthorizedParty>(totalCapacity);

        for (var i = 0; i < topLevelCount; i++)
        {
            var party = authorizedParties.AuthorizedParties[i];

            flattenedList.Add(new AuthorizedParty
            {
                Party = party.Party,
                PartyId = party.PartyId,
                ParentParty = null,
                AuthorizedResources = party.AuthorizedResources.Count > 0
                    ? [.. party.AuthorizedResources]
                    : EmptyStringList,
                AuthorizedRolesAndAccessPackages = party.AuthorizedRolesAndAccessPackages.Count > 0
                    ? [.. party.AuthorizedRolesAndAccessPackages]
                    : EmptyStringList,
                AuthorizedInstances = party.AuthorizedInstances.Count > 0
                    ? [.. party.AuthorizedInstances]
                    : [],
                SubParties = EmptySubPartiesList
            });

            if (!(party.SubParties?.Count > 0)) continue;
            var subCount = party.SubParties.Count;
            for (var j = 0; j < subCount; j++)
            {
                var subParty = party.SubParties[j];
                flattenedList.Add(new AuthorizedParty
                {
                    Party = subParty.Party,
                    PartyId = subParty.PartyId,
                    ParentParty = party.Party,
                    AuthorizedResources = subParty.AuthorizedResources.Count > 0
                        ? [.. subParty.AuthorizedResources]
                        : EmptyStringList,
                    AuthorizedRolesAndAccessPackages = subParty.AuthorizedRolesAndAccessPackages.Count > 0
                        ? [.. subParty.AuthorizedRolesAndAccessPackages]
                        : EmptyStringList,
                    AuthorizedInstances = subParty.AuthorizedInstances.Count > 0
                        ? [.. subParty.AuthorizedInstances]
                        : [],
                    SubParties = EmptySubPartiesList
                });
            }
        }

        return new AuthorizedPartiesResult
        {
            AuthorizedParties = flattenedList
        };
    }

    private async Task<AuthorizedPartiesResult> PerformAuthorizedPartiesRequest(AuthorizedPartiesRequest authorizedPartiesRequest,
        CancellationToken cancellationToken)
    {
        var authorizedPartiesDto = await SendAuthorizedPartiesRequest(authorizedPartiesRequest, cancellationToken);
        // System users might have no rights whatsoever, which is not an error condition
        // Other user types (persons, SI users) will always be able to represent themselves as a minimum
        if (authorizedPartiesDto is null || (authorizedPartiesDto.Count == 0 && authorizedPartiesRequest.Type != SystemUserIdentifier.Prefix))
        {
            _logger.LogWarning("Empty authorized parties for party T={Type} V={Value}", authorizedPartiesRequest.Type, authorizedPartiesRequest.Value);
            throw new UpstreamServiceException("access-management returned no authorized parties, missing Altinn profile?");
        }

        return AuthorizedPartiesHelper.CreateAuthorizedPartiesResult(authorizedPartiesDto, authorizedPartiesRequest);
    }

    private async Task<DialogSearchAuthorizationResult> PerformDialogSearchAuthorization(DialogSearchAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var partyIdentifier = request.Claims.GetEndUserPartyIdentifier() ?? throw new UnreachableException();
        var authorizedParties = await GetAuthorizedParties(partyIdentifier, flatten: true, cancellationToken: cancellationToken);

        var result = await AuthorizationHelper.ResolveDialogSearchAuthorization(
            authorizedParties,
            request.ConstraintParties,
            request.ConstraintServiceResources,
            GetAllSubjectResources,
            cancellationToken);

        return await PopulateDialogIdsFromInstanceDelegationIds(result, cancellationToken);
    }

    private async Task<DialogSearchAuthorizationResult> PopulateDialogIdsFromInstanceDelegationIds(DialogSearchAuthorizationResult result, CancellationToken cancellationToken)
    {
        if (result.AltinnAppInstanceIds.Count == 0)
        {
            return result;
        }

        result.DialogIds = await _db.DialogServiceOwnerLabels
            .Where(l => result.AltinnAppInstanceIds.Contains(l.Value))
            .Select(l => l.DialogServiceOwnerContext.DialogId)
            .ToListAsync(cancellationToken: cancellationToken);

        return result;
    }

    private async Task<List<SubjectResource>> GetAllSubjectResources(CancellationToken cancellationToken) =>
        await _subjectResourcesCache.GetOrSetAsync(nameof(SubjectResource), async ct =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
                return await dbContext.SubjectResources.AsNoTracking().ToListAsync(cancellationToken: ct);
            },
            token: cancellationToken);

    private async Task<DialogDetailsAuthorizationResult> PerformDialogDetailsAuthorization(
        DialogDetailsAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var xacmlJsonRequest = DecisionRequestHelper.CreateDialogDetailsRequest(request);
        var xacmlJsonResponse = await SendPdpRequest(xacmlJsonRequest, cancellationToken);
        LogIfIndeterminate(xacmlJsonResponse, xacmlJsonRequest);

        return DecisionRequestHelper.CreateDialogDetailsResponse(request.AltinnActions, xacmlJsonResponse);
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
