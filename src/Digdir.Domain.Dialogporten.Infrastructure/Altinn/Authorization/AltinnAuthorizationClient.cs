using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
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
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;
using Constants = Digdir.Domain.Dialogporten.Domain.Common.Constants;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal sealed partial class AltinnAuthorizationClient : IAltinnAuthorization
{
    private const string AuthorizeUrl = "authorization/api/v1/authorize";
    private const string AuthorizedPartiesBaseUrl = "/accessmanagement/api/v1/resourceowner/authorizedparties";

    private readonly HttpClient _httpClient;
    private readonly IFusionCache _pdpCache;
    private readonly IFusionCache _partiesCache;
    private readonly IFusionCache _subjectResourcesCache;
    private readonly IUser _user;
    private readonly IDialogDbContext _db;
    private readonly IServiceResourceMinimumAuthenticationLevelResolver _serviceResourceMinimumAuthenticationLevelResolver;
    private readonly IPartyResourceReferenceRepository _partyResourceReferenceRepository;
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IOptionsMonitor<ApplicationSettings> _applicationSettings;
    private readonly IPartyNameRegistry _partyNameRegistry;

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
        IServiceResourceMinimumAuthenticationLevelResolver serviceResourceMinimumAuthenticationLevelResolver,
        IPartyResourceReferenceRepository partyResourceReferenceRepository,
        ILogger<AltinnAuthorizationClient> logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptionsMonitor<ApplicationSettings> applicationSettings,
        IPartyNameRegistry partyNameRegistry
    )
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(serviceResourceMinimumAuthenticationLevelResolver);
        ArgumentNullException.ThrowIfNull(partyResourceReferenceRepository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        ArgumentNullException.ThrowIfNull(applicationSettings);
        ArgumentNullException.ThrowIfNull(partyNameRegistry);

        var pdpCache = cacheProvider.GetCache(nameof(Authorization));
        ArgumentNullException.ThrowIfNull(pdpCache);

        var partiesCache = cacheProvider.GetCache(nameof(AuthorizedPartiesResult));
        ArgumentNullException.ThrowIfNull(partiesCache);

        var subjectResourcesCache = cacheProvider.GetCache(nameof(SubjectResource));
        ArgumentNullException.ThrowIfNull(subjectResourcesCache);

        _httpClient = client;
        _pdpCache = pdpCache;
        _partiesCache = partiesCache;
        _subjectResourcesCache = subjectResourcesCache;
        _user = user;
        _db = db;
        _serviceResourceMinimumAuthenticationLevelResolver = serviceResourceMinimumAuthenticationLevelResolver;
        _partyResourceReferenceRepository = partyResourceReferenceRepository;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _applicationSettings = applicationSettings;
        _partyNameRegistry = partyNameRegistry;
    }

    public async Task<DialogDetailsAuthorizationResult> GetDialogDetailsAuthorization(
        DialogEntity dialogEntity,
        CancellationToken cancellationToken = default)
    {
        var instanceRef = InstanceRef.FromDialog(dialogEntity);

        var request = new DialogDetailsAuthorizationRequest
        {
            ClaimsPrincipal = _user.GetPrincipal(),
            ServiceResource = dialogEntity.ServiceResource,
            InstanceRef = instanceRef,
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
        CancellationToken cancellationToken = default) =>
        await GetAuthorizedPartiesInternal(new AuthorizedPartiesRequest(authenticatedParty), flatten, cancellationToken);

    public async Task<AuthorizedPartiesResult> GetAuthorizedPartiesForLookup(
        IPartyIdentifier authenticatedParty,
        List<string> constraintParties,
        CancellationToken cancellationToken = default)
    {
        return await GetAuthorizedPartiesInternal(
            new AuthorizedPartiesRequest(
                authenticatedParty,
                includeAccessPackages: true,
                includeRoles: true,
                includeResources: true,
                includeInstances: true,
                partyFilter: CreatePartyFilters(constraintParties)),
            flatten: true,
            cancellationToken);
    }

    private async Task<AuthorizedPartiesResult> GetAuthorizedPartiesInternal(
        AuthorizedPartiesRequest authorizedPartiesRequest,
        bool flatten,
        CancellationToken cancellationToken)
    {
        AuthorizedPartiesResult authorizedParties;


        // Self-identified/Feide users are not currently supported in the access management API, so we need to emulate it.
        // Assume they can only represent themselves, and have no delegated rights.

        // We currently do not have any support in the Register API to resolve the name of self-identified users,
        // so we need to get this from the request context, which means this will ONLY work in end-user contexts
        // where there is a end-user token available, ie. not in service owner contexts (using ?EndUserId=...) or
        // service contexts.
        switch (authorizedPartiesRequest.PartyIdentifier)
        {
            case IdportenEmailUserIdentifier
                when !_applicationSettings.CurrentValue.FeatureToggle.UseAccessManagementForIdportenEmailUsers:
            case AltinnSelfIdentifiedUserIdentifier
                when !_applicationSettings.CurrentValue.FeatureToggle.UseAccessManagementForAltinnSelfIdentifiedUsers:
            case FeideUserIdentifier
                when !_applicationSettings.CurrentValue.FeatureToggle.UseAccessManagementForFeideUsers:
                return GetSyntheticAuthorizedPartiesResultForSelfIdentifiedUser(authorizedPartiesRequest);

            // ReSharper disable once RedundantEmptySwitchSection
            default:
                break;
        }

        using (_logger.TimeOperation(nameof(GetAuthorizedParties)))
        {
            authorizedParties = await _partiesCache.GetOrSetAsync(authorizedPartiesRequest.GenerateCacheKey(), async token
                => await PerformAuthorizedPartiesRequest(authorizedPartiesRequest, token), token: cancellationToken);
        }

        var flattenedParties = authorizedParties.Flatten();
        var nameByActorId = flattenedParties
            .AuthorizedParties
            .Where(x => x.Party is not null && x.Name is not null)
            .DistinctBy(x => x.Party)
            .Select(x => (x.Party, x.Name))
            .ToDictionary();

        _partyNameRegistry.CacheNames(nameByActorId);

        return flatten ? flattenedParties : authorizedParties;
    }

    private AuthorizedPartiesResult GetSyntheticAuthorizedPartiesResultForSelfIdentifiedUser(
        AuthorizedPartiesRequest authorizedPartiesRequest)
    {
        var authorizedPartiesResultDto = new AuthorizedPartiesResultDto
        {
            PartyUuid = _user.GetPrincipal().TryGetPartyUuid(out var uuid) ? uuid : throw new UnreachableException("Expected party UUID to be present in current principal"),
            PartyId = _user.GetPrincipal().TryGetPartyId(out var partyId) ? partyId : throw new UnreachableException("Expected party ID to be present in current principal"),
            Name = authorizedPartiesRequest.PartyIdentifier.Id,
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

    public async Task<bool> HasListAuthorizationForDialog(DialogEntity dialog, CancellationToken cancellationToken)
    {
        var authorizedResourcesForSearch = await GetAuthorizedResourcesForSearch(
            [dialog.Party], [dialog.ServiceResource], cancellationToken);

        return authorizedResourcesForSearch.ResourcesByParties.Count > 0
               || authorizedResourcesForSearch.DialogIds.Contains(dialog.Id);
    }

    public bool UserHasRequiredAuthLevel(int minimumAuthenticationLevel) =>
        minimumAuthenticationLevel <= _user.GetPrincipal().GetAuthenticationLevel();

    public async Task<bool> UserHasRequiredAuthLevel(string serviceResource, CancellationToken cancellationToken) =>
        UserHasRequiredAuthLevel(await _serviceResourceMinimumAuthenticationLevelResolver
            .GetMinimumAuthenticationLevel(serviceResource, cancellationToken));

    private async Task<AuthorizedPartiesResult> PerformAuthorizedPartiesRequest(AuthorizedPartiesRequest authorizedPartiesRequest,
        CancellationToken cancellationToken)
    {
        var authorizedPartiesDto = await SendAuthorizedPartiesRequest(authorizedPartiesRequest, cancellationToken);
        // System users might have no rights whatsoever, which is not an error condition. Other user types (persons, SI users)
        // will always be able to represent themselves as a minimum, unless a party filter is supplied
        if (authorizedPartiesDto is null || (
                authorizedPartiesDto.Count == 0
                && authorizedPartiesRequest.PartyIdentifier is not SystemUserIdentifier
                && authorizedPartiesRequest.PartyFilter.Count == 0))
        {
            _logger.LogWarning("Empty authorized parties for party T={Type} V={Value}", authorizedPartiesRequest.PartyIdentifier.Prefix(), authorizedPartiesRequest.PartyIdentifier.Id);
            throw new UpstreamServiceException("access-management returned no authorized parties, missing Altinn profile?");
        }

        return AuthorizedPartiesHelper.CreateAuthorizedPartiesResult(authorizedPartiesDto, authorizedPartiesRequest);
    }

    private async Task<DialogSearchAuthorizationResult> PerformDialogSearchAuthorization(DialogSearchAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var partyIdentifier = request.Claims.GetEndUserPartyIdentifier();
        if (partyIdentifier is null)
        {
            var (userType, externalId) = _user.GetPrincipal().GetUserType();
            var safeExternalId = userType is DialogUserType.Values.Person
                or DialogUserType.Values.ServiceOwnerOnBehalfOfPerson
                or DialogUserType.Values.IdportenEmailIdentifiedUser
                ? "<redacted>"
                : externalId;
            throw new UnreachableException(
                $"GetEndUserPartyIdentifier returned null. UserType={userType}, ExternalId={safeExternalId}");
        }

        var authorizedPartiesRequest = new AuthorizedPartiesRequest(
            partyIdentifier,
            includeAccessPackages: true,
            includeRoles: true,
            includeResources: true,
            includeInstances: true,
            partyFilter: CreatePartyFilters(request.ConstraintParties));

        var authorizedParties = await GetAuthorizedPartiesInternal(
            authorizedPartiesRequest,
            flatten: true,
            cancellationToken);

        var result = await AuthorizationHelper.ResolveDialogSearchAuthorization(
            authorizedParties,
            request.ConstraintParties,
            request.ConstraintServiceResources,
            GetAllSubjectResources,
            cancellationToken);

        var applicationSettings = _applicationSettings.CurrentValue;
        var featureToggle = applicationSettings.FeatureToggle;
        if (featureToggle.UsePartyResourcePruning)
        {
            var partyResourcePruningLimits = applicationSettings.Limits.PartyResourcePruning;
            await AuthorizationHelper.PruneUnreferencedResources(
                result,
                _partyResourceReferenceRepository,
                partyResourcePruningLimits.MinResourcesPruningThreshold,
                cancellationToken);
        }

        await PopulateDialogIdsFromInstanceRefs(
            result,
            authorizedParties,
            request.ConstraintServiceResources,
            cancellationToken);

        return result;
    }

    private async Task PopulateDialogIdsFromInstanceRefs(
        DialogSearchAuthorizationResult result,
        AuthorizedPartiesResult authorizedParties,
        List<string> constraintServiceResources,
        CancellationToken cancellationToken)
    {
        if (authorizedParties.AuthorizedParties.Count == 0)
        {
            return;
        }

        var constraintResources = constraintServiceResources.Count == 0
            ? null
            : new HashSet<string>(constraintServiceResources, StringComparer.OrdinalIgnoreCase);

        var directDialogIds = new HashSet<Guid>();
        var lookupLabels = new HashSet<string>(StringComparer.Ordinal);

        foreach (var party in authorizedParties.AuthorizedParties)
        {
            var partyAuthorizedInstances = party.AuthorizedInstances
                .Where(instance =>
                    constraintResources is null
                    || constraintResources.Contains($"{Constants.ServiceResourcePrefix}{instance.ResourceId}"))
                .ToList();

            if (partyAuthorizedInstances.Count == 0)
            {
                continue;
            }

            foreach (var authorizedInstance in partyAuthorizedInstances)
            {
                if (string.IsNullOrWhiteSpace(authorizedInstance.InstanceRef))
                {
                    continue;
                }

                if (!InstanceRef.TryParse(authorizedInstance.InstanceRef, out var parsedInstanceRef))
                {
                    continue;
                }

                var instanceRef = parsedInstanceRef.Value;
                if (instanceRef.Type is InstanceRefType.DialogId)
                {
                    directDialogIds.Add(instanceRef.Id);
                    continue;
                }

                lookupLabels.Add(instanceRef.ToLookupLabel());
            }
        }

        if (lookupLabels.Count > 0)
        {
            var dialogIdsFromLabels = await _db.DialogServiceOwnerLabels
                .Where(l => lookupLabels.Contains(l.Value))
                .Select(l => l.DialogServiceOwnerContext.DialogId)
                .ToListAsync(cancellationToken: cancellationToken);
            directDialogIds.UnionWith(dialogIdsFromLabels);
        }

        if (result.DialogIds.Count > 0)
        {
            directDialogIds.UnionWith(result.DialogIds);
        }

        result.DialogIds = [.. directDialogIds];
    }

    private async Task<List<SubjectResource>> GetAllSubjectResources(CancellationToken cancellationToken) =>
        await _subjectResourcesCache.GetOrSetAsync(nameof(SubjectResource), async ct =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
                return await dbContext.SubjectResources.AsNoTracking().ToListAsync(cancellationToken: ct);
            },
            token: cancellationToken);

    private static List<AuthorizedPartyFilter> CreatePartyFilters(List<string> constraintParties)
    {
        if (constraintParties.Count == 0)
        {
            return [];
        }

        var filters = new List<AuthorizedPartyFilter>(constraintParties.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var party in constraintParties)
        {
            if (string.IsNullOrWhiteSpace(party)
                || !PartyIdentifier.TryParse(party.AsSpan(), out var identifier))
            {
                continue;
            }

            var type = identifier.Prefix();
            var filterKey = $"{type}:{identifier.Id}";
            if (!seen.Add(filterKey))
            {
                continue;
            }

            filters.Add(new AuthorizedPartyFilter
            {
                Type = type,
                Value = identifier.Id
            });
        }

        return filters;
    }

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
            BuildAuthorizedPartiesUrl(authorizedPartiesRequest), authorizedPartiesRequest, cancellationToken);

    private string BuildAuthorizedPartiesUrl(AuthorizedPartiesRequest request)
    {
        var featureToggle = _applicationSettings.CurrentValue.FeatureToggle;
        var autoQueryParams = featureToggle.UseAltinnAutoAuthorizedPartiesQueryParameters
            ? "&includePartiesViaKeyRoles=auto&includeSubParties=auto&includeInactiveParties=auto"
            : string.Empty;

        return $"{AuthorizedPartiesBaseUrl}?includeAltinn2=true{autoQueryParams}" +
               $"&includeAccessPackages={(request.IncludeAccessPackages ? "true" : "false")}" +
               $"&includeRoles={(request.IncludeRoles ? "true" : "false")}" +
               $"&includeResources={(request.IncludeResources ? "true" : "false")}" +
               $"&includeInstances={(request.IncludeInstances ? "true" : "false")}";
    }

    private async Task<T?> SendRequest<T>(string url, object request, CancellationToken cancellationToken)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var requestJson = JsonSerializer.Serialize(request, SerializerOptions);
            LogAuthorizationRequest(url, requestJson);
        }

        return await _httpClient.PostAsJsonEnsuredAsync<T>(url, request, serializerOptions: SerializerOptions, cancellationToken: cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Authorization request to {Url}: {RequestJson}")]
    private partial void LogAuthorizationRequest(string url, string requestJson);
}
