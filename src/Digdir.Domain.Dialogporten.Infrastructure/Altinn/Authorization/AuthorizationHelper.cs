using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;
using Digdir.Domain.Dialogporten.Domain.Common;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal static class AuthorizationHelper
{
    /// <summary>
    /// This method resolves subjects (ie. roles and access packages) based on the authorized parties and constraints provided,
    /// returning a list of all the (unique) resources associated with each party.
    ///
    /// Additionally, it collects Altinn app instance delegations for parties that have them, returning them as a intermediate
    /// list of instance ids that can later be used to determine the actual dialog ids for these app instances. This is performed
    /// in this method as well, to avoid having to loop/filter over (the potentially large) list of parties multiple times.
    /// </summary>
    /// <param name="authorizedParties">
    /// Authorized parties from Access Management. This input is treated as immutable and must not be mutated.
    /// </param>
    /// <param name="constraintParties">
    /// Optional party constraints (full party URNs). When provided, only matching parties are processed.
    /// </param>
    /// <param name="constraintResources">
    /// Optional resource constraints (full resource URNs). When provided, only matching resources are kept.
    /// </param>
    /// <param name="getAllSubjectResources">
    /// Callback that resolves all known subject-resource mappings (roles/access packages to resources).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="DialogSearchAuthorizationResult"/> containing resolved resources by party and delegated instance IDs.
    /// </returns>
    public static async Task<DialogSearchAuthorizationResult> ResolveDialogSearchAuthorization(
        AuthorizedPartiesResult authorizedParties,
        List<string> constraintParties,
        List<string> constraintResources,
        Func<CancellationToken, Task<List<SubjectResource>>> getAllSubjectResources,
        CancellationToken cancellationToken)
    {
        var result = new DialogSearchAuthorizationResult
        {
            // Pre-size with a reasonable capacity to reduce re-allocations.
            ResourcesByParties = new Dictionary<string, HashSet<string>>(100)
        };

        if (authorizedParties.AuthorizedParties.Count == 0)
        {
            return result;
        }

        // Create HashSets from constraints for efficient lookups.
        var constraintPartiesSet = constraintParties.Count > 0 ? new HashSet<string>(constraintParties) : null;
        var constraintResourcesSet = constraintResources.Count > 0 ? new HashSet<string>(constraintResources) : null;

        // Pre-filter parties to a relevant subset to avoid filtering the same large list multiple times.
        var relevantParties = authorizedParties.AuthorizedParties
            .Where(p => constraintPartiesSet is null || constraintPartiesSet.Contains(p.Party))
            .ToList();

        if (relevantParties.Count == 0)
        {
            return result;
        }

        // Step 1: Collect all unique subjects (roles/access packages) from the relevant parties.
        // This is required to efficiently fetch only the necessary subject-to-resource mappings.
        var uniqueSubjects = new HashSet<string>(100);
        foreach (var party in relevantParties)
        {
            foreach (var roleOrAccessPackage in party.AuthorizedRolesAndAccessPackages)
            {
                uniqueSubjects.Add(roleOrAccessPackage);
            }
        }

        // Step 2: Build a lookup dictionary that maps each subject to its corresponding resources.
        var subjectToResources = new Dictionary<string, HashSet<string>>();
        if (uniqueSubjects.Count > 0)
        {
            var subjectResources = await getAllSubjectResources(cancellationToken);
            foreach (var sr in subjectResources)
            {
                // The mapping is built only from subjects and resources that satisfy all constraints.
                if (!uniqueSubjects.Contains(sr.Subject) ||
                    (constraintResourcesSet != null && !constraintResourcesSet.Contains(sr.Resource)))
                {
                    continue;
                }

                if (!subjectToResources.TryGetValue(sr.Subject, out var resources))
                {
                    resources = new HashSet<string>();
                    subjectToResources[sr.Subject] = resources;
                }

                resources.Add(sr.Resource);
            }
        }

        // Step 3: Process each relevant party to aggregate all its authorizations.
        // This single loop handles role-based resources, direct resources, and instance delegations.
        foreach (var party in relevantParties)
        {
            var partyResources = new HashSet<string>();

            // Aggregate resources granted via roles and access packages.
            foreach (var role in party.AuthorizedRolesAndAccessPackages)
            {
                if (subjectToResources.TryGetValue(role, out var resourcesFromRole))
                {
                    partyResources.UnionWith(resourcesFromRole);
                }
            }

            // Aggregate resources granted directly to the party.
            foreach (var resource in party.AuthorizedResources)
            {
                if (constraintResourcesSet is null || constraintResourcesSet.Contains(resource))
                {
                    partyResources.Add(resource);
                }
            }

            // If the party has been granted access to any resources, add them to the result.
            if (partyResources.Count > 0)
            {
                result.ResourcesByParties[party.Party] = partyResources;
            }

            // Handle app instance delegations from Altinn Access Management.
            foreach (var instance in party.AuthorizedInstances)
            {
                if (!instance.ResourceId.StartsWith(Constants.AppResourceIdPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if ((constraintResourcesSet is null
                    || constraintResourcesSet.Contains(Constants.ServiceResourcePrefix + instance.ResourceId))
                    && Guid.TryParse(instance.InstanceId, out _))
                {
                    result.AltinnAppInstanceIds.Add(
                        $"{Constants.ServiceContextInstanceIdPrefix}{party.PartyId}/{instance.InstanceId}");
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Prunes resolved service resources for each party by intersecting authorized resources with
    /// existing party-service references. This is an optimization step before the actual dialog search,
    /// to avoid spending time looking for service resources not actually referenced by the give party.
    /// </summary>
    /// <param name="result">Resolved dialog search authorization result to prune in-place.</param>
    /// <param name="partyResourceReferenceRepository">
    /// Repository used to resolve existing service resources per party.
    /// </param>
    /// <param name="maxPartiesForPruning">
    /// Optional upper bound for number of parties eligible for pruning.
    /// </param>
    /// <param name="resourcePruningThreshold">
    /// Minimum number of distinct service resources required before pruning is attempted.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task PruneUnreferencedResources(
        DialogSearchAuthorizationResult result,
        IPartyResourceReferenceRepository partyResourceReferenceRepository,
        int? maxPartiesForPruning,
        int resourcePruningThreshold,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(partyResourceReferenceRepository);

        if (result.ResourcesByParties.Count == 0)
        {
            return;
        }

        if (maxPartiesForPruning is > 0
            && result.ResourcesByParties.Count > maxPartiesForPruning.Value)
        {
            return;
        }

        var distinctResources = result.ResourcesByParties
            .Values
            .SelectMany(x => x)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (distinctResources.Length <= resourcePruningThreshold)
        {
            return;
        }

        var existingResourcesByParty = await partyResourceReferenceRepository.GetReferencedResourcesByParty(
            [.. result.ResourcesByParties.Keys],
            distinctResources,
            cancellationToken);

        var prunedEntries = result.ResourcesByParties
            .Select(x => (x.Key, Resources: existingResourcesByParty.TryGetValue(x.Key, out var existingServices)
                ? x.Value.Intersect(existingServices, StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : []))
            .Where(x => x.Resources.Count > 0)
            .ToList();

        result.ResourcesByParties.Clear();
        foreach (var (party, services) in prunedEntries)
        {
            result.ResourcesByParties[party] = services;
        }
    }
}
