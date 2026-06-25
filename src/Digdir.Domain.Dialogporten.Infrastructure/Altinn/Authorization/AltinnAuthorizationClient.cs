using System.Diagnostics;
using System.Security.Claims;
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
        bool includeDialogIds = true,
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
        return await PerformDialogSearchAuthorization(request, includeDialogIds, cancellationToken);
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
        // Currently, access management is unable to properly leverage party filters on system users. Disable
        // filtering here to ensure better cache hit rate for system users iterating over many parties.
        if (authorizedPartiesRequest.PartyIdentifier is SystemUserIdentifier
            && !_applicationSettings.CurrentValue.FeatureToggle.EnablePartyFiltersForSystemUsers)
        {
            authorizedPartiesRequest.PartyFilter.Clear();
        }

        // Party filtering for SI users is mostly useless, as it very often is just one or two
        // parties. Disable by default to reduce cache cardinality.
        if (authorizedPartiesRequest.PartyIdentifier is IdportenEmailUserIdentifier
            && !_applicationSettings.CurrentValue.FeatureToggle.EnablePartyFiltersForEmailUsers)
        {
            authorizedPartiesRequest.PartyFilter.Clear();
        }

        AuthorizedPartiesResult authorizedParties;
        using (_logger.TimeOperation(nameof(GetAuthorizedParties)))
        {
            // Party cache for SI users is problematic due to merging of legacy SI users and e-mail users,
            // since this flow assumes that the user is immediately able to load the updated party list. The
            // list is cheap to generate in AM, so we pay the cost of doing this lookup for all SI user requests
            authorizedParties = authorizedPartiesRequest.PartyIdentifier is IdportenEmailUserIdentifier
                && !_applicationSettings.CurrentValue.FeatureToggle.EnablePartyCacheForEmailUsers
                ? await PerformAuthorizedPartiesRequest(authorizedPartiesRequest, cancellationToken)
                : await _partiesCache.GetOrSetAsync(
                    authorizedPartiesRequest.GenerateCacheKey(),
                    async token => await PerformAuthorizedPartiesRequest(authorizedPartiesRequest, token),
                    token: cancellationToken
                );
        }

        return flatten ? authorizedParties.Flatten() : authorizedParties;
    }

    public async Task<bool> HasListAuthorizationForDialog(DialogEntity dialog, CancellationToken cancellationToken)
    {
        var authorizedResourcesForSearch = await GetAuthorizedResourcesForSearch(
            [dialog.Party], [dialog.ServiceResource], cancellationToken: cancellationToken);

        return authorizedResourcesForSearch.ResourcesByParties.Count > 0
               || authorizedResourcesForSearch.DialogIds.Contains(dialog.Id);
    }

    public bool UserHasRequiredAuthLevel(int minimumAuthenticationLevel) =>
        minimumAuthenticationLevel <= _user.GetPrincipal().GetAuthenticationLevel();

    public async Task<bool> UserHasRequiredAuthLevel(string serviceResource, CancellationToken cancellationToken) =>
        UserHasRequiredAuthLevel(await _serviceResourceMinimumAuthenticationLevelResolver
            .GetMinimumAuthenticationLevel(serviceResource, cancellationToken));

    private async Task<AuthorizedPartiesResult> PerformAuthorizedPartiesRequest(
        AuthorizedPartiesRequest authorizedPartiesRequest,
        CancellationToken cancellationToken
    )
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

        var result = AuthorizedPartiesHelper.CreateAuthorizedPartiesResult(
            authorizedPartiesDto,
            authorizedPartiesRequest
        );
        var currentUser = result.AuthorizedParties.Find(p => p.IsCurrentEndUser);
        if (currentUser is not null)
        {
            _partyNameRegistry.CacheName(currentUser.Party, currentUser.Name);
        }

        return result;
    }

    private async Task<DialogSearchAuthorizationResult> PerformDialogSearchAuthorization(DialogSearchAuthorizationRequest request, bool includeDialogIds, CancellationToken cancellationToken)
    {
        var partyIdentifier = _user.GetPrincipal().GetEndUserPartyIdentifierOrThrow();

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

        // Pruning intersects the authorized resources with those actually referenced by Dialogporten.
        // This is a functional requirement (not just a search optimization): the authorized service
        // resources API relies on this pipeline to return a subset of the referenced resource catalogue.
        var partyResourcePruningLimits = _applicationSettings.CurrentValue.Limits.PartyResourcePruning;
        await AuthorizationHelper.PruneUnreferencedResources(
            result,
            _partyResourceReferenceRepository,
            partyResourcePruningLimits.MinResourcesPruningThreshold,
            cancellationToken);

        if (includeDialogIds)
        {
            await PopulateDialogIdsFromInstanceRefs(
                result,
                authorizedParties,
                request.ConstraintServiceResources,
                cancellationToken);
        }

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
