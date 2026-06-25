using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal static class AuthorizationHelper
{
    /// <summary>
    /// This method resolves subjects (ie. roles and access packages) based on the authorized parties and constraints provided,
    /// returning a list of all the (unique) resources associated with each party.
    ///
    /// Additionally, it collects Altinn app instance delegations for parties that have them, returning them as an intermediate
    /// list of service owner labels for app instances that can later be used to determine the actual dialog ids. This is performed
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
            ResourcesByParties = new Dictionary<string, IReadOnlySet<string>>(100)
        };

        if (authorizedParties.AuthorizedParties.Count == 0)
        {
            return result;
        }

        // Create HashSets from constraints for efficient lookups.
        var constraintPartiesSet = constraintParties.Count > 0
            ? new HashSet<string>(constraintParties, StringComparer.OrdinalIgnoreCase)
            : null;
        var constraintResourcesSet = constraintResources.Count > 0
            ? new HashSet<string>(constraintResources, StringComparer.OrdinalIgnoreCase)
            : null;

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
        var uniqueSubjects = new HashSet<string>(100, StringComparer.OrdinalIgnoreCase);
        foreach (var party in relevantParties)
        {
            foreach (var roleOrAccessPackage in party.AuthorizedRolesAndAccessPackages)
            {
                uniqueSubjects.Add(roleOrAccessPackage);
            }
        }

        // Step 2: Build a lookup dictionary that maps each subject to its corresponding resources.
        var subjectToResources = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
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
                    resources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    subjectToResources[sr.Subject] = resources;
                }

                resources.Add(sr.Resource);
            }
        }

        // Step 3: Process each relevant party to aggregate all its authorizations.
        // Many parties share the same set of roles/access packages (e.g. the same role across every party a user
        // represents), which resolve to the same resources. We memoize the role-derived resource set per role-set
        // so that union is computed once and reused, instead of recomputing it for every party. When a party has
        // no directly-granted resources we reuse the memoized instance by reference: downstream consumers treat
        // these sets as read-only and group parties by resource-set equality, so sharing is safe and reduces work.
        var roleDerivedResourcesByRoleSet = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var party in relevantParties)
        {
            var roleDerivedResources = ResolveRoleDerivedResources(
                party.AuthorizedRolesAndAccessPackages, subjectToResources, roleDerivedResourcesByRoleSet);

            HashSet<string> partyResources;
            if (party.AuthorizedResources.Count == 0)
            {
                if (roleDerivedResources.Count == 0)
                {
                    continue;
                }

                // No directly-granted resources: reuse the shared, memoized role-derived set as-is.
                partyResources = roleDerivedResources;
            }
            else
            {
                // Copy the memoized role-derived set before adding this party's directly-granted resources.
                partyResources = new HashSet<string>(roleDerivedResources, StringComparer.OrdinalIgnoreCase);
                foreach (var resource in party.AuthorizedResources)
                {
                    if (constraintResourcesSet is null || constraintResourcesSet.Contains(resource))
                    {
                        partyResources.Add(resource);
                    }
                }

                if (partyResources.Count == 0)
                {
                    continue;
                }
            }

            result.ResourcesByParties[party.Party] = partyResources;
        }

        return result;
    }

    private static readonly HashSet<string> EmptyResources = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the (memoized) union of resources granted by the given set of roles/access packages. Identical
    /// role-sets share a single computed instance. The returned set must be treated as read-only by callers.
    /// </summary>
    private static HashSet<string> ResolveRoleDerivedResources(
        List<string> rolesAndAccessPackages,
        Dictionary<string, HashSet<string>> subjectToResources,
        Dictionary<string, HashSet<string>> roleDerivedResourcesByRoleSet)
    {
        if (rolesAndAccessPackages.Count == 0)
        {
            return EmptyResources;
        }

        // Single role is the common case; use it directly as the key to avoid sorting/joining allocations.
        var roleSetKey = rolesAndAccessPackages.Count == 1
            ? rolesAndAccessPackages[0]
            : BuildRoleSetKey(rolesAndAccessPackages);

        if (roleDerivedResourcesByRoleSet.TryGetValue(roleSetKey, out var cached))
        {
            return cached;
        }

        var resources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var subject in rolesAndAccessPackages)
        {
            if (subjectToResources.TryGetValue(subject, out var subjectResources))
            {
                resources.UnionWith(subjectResources);
            }
        }

        roleDerivedResourcesByRoleSet[roleSetKey] = resources;
        return resources;
    }

    private static string BuildRoleSetKey(List<string> rolesAndAccessPackages)
    {
        var sorted = rolesAndAccessPackages.ToArray();
        // OrdinalIgnoreCase to match the role-set memo dictionary's comparer (so case-variant role sets resolve
        // to the same key). Unit separator is not a valid character in role/access-package URNs, so the joined
        // key is unambiguous.
        Array.Sort(sorted, StringComparer.OrdinalIgnoreCase);
        return string.Join('\u001f', sorted);
    }

    /// <summary>
    /// Prunes resolved service resources for each party by intersecting authorized resources with
    /// existing party-service references. This is an optimization step before the actual dialog search,
    /// to avoid spending time looking for service resources not actually referenced by any given party.
    /// </summary>
    /// <param name="result">Resolved dialog search authorization result to prune in-place.</param>
    /// <param name="partyResourceReferenceRepository">
    /// Repository used to resolve existing service resources per party.
    /// </param>
    /// <param name="minResourcesPruningThreshold">
    /// Minimum number of distinct service resources required before pruning is attempted.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task PruneUnreferencedResources(
        DialogSearchAuthorizationResult result,
        IPartyResourceReferenceRepository partyResourceReferenceRepository,
        int minResourcesPruningThreshold,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(partyResourceReferenceRepository);

        if (result.ResourcesByParties.Count == 0)
        {
            return;
        }

        // Compute the distinct authorized resources across all parties. Many parties share the SAME set instance
        // (ResolveDialogSearchAuthorization memoizes role-derived sets), so dedupe by instance reference first and
        // union only the distinct instances, instead of enumerating every party's resources.
        var distinctSetInstances = new HashSet<IReadOnlySet<string>>(ReferenceEqualityComparer.Instance);
        foreach (var resources in result.ResourcesByParties.Values)
        {
            distinctSetInstances.Add(resources);
        }

        var distinctResources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var instance in distinctSetInstances)
        {
            distinctResources.UnionWith(instance);
        }

        if (distinctResources.Count <= minResourcesPruningThreshold)
        {
            return;
        }

        var existingResourcesByParty = await partyResourceReferenceRepository.GetReferencedResourcesByParty(
            [.. result.ResourcesByParties.Keys],
            distinctResources,
            cancellationToken);

        var prunedEntries = new List<(string Party, HashSet<string> Resources)>(result.ResourcesByParties.Count);
        foreach (var (party, authorizedResources) in result.ResourcesByParties)
        {
            if (!existingResourcesByParty.TryGetValue(party, out var existingServices) || existingServices.Count == 0)
            {
                continue;
            }

            // Intersect two sets directly (probe the larger with the smaller) instead of Enumerable.Intersect,
            // which would rebuild a set from the second operand on every party. Both operands use
            // StringComparer.OrdinalIgnoreCase (authorized sets here, referenced sets from PartyResourceRepository),
            // so membership is comparer-stable regardless of which one is the larger/probed set.
            IReadOnlySet<string> smaller, larger;
            if (authorizedResources.Count <= existingServices.Count)
            {
                smaller = authorizedResources;
                larger = existingServices;
            }
            else
            {
                smaller = existingServices;
                larger = authorizedResources;
            }

            var pruned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var resource in smaller)
            {
                if (larger.Contains(resource))
                {
                    pruned.Add(resource);
                }
            }

            if (pruned.Count > 0)
            {
                prunedEntries.Add((party, pruned));
            }
        }

        result.ResourcesByParties.Clear();
        foreach (var (party, services) in prunedEntries)
        {
            result.ResourcesByParties[party] = services;
        }
    }
}
