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
    /// <param name="authorizedParties"></param>
    /// <param name="constraintParties"></param>
    /// <param name="constraintResources"></param>
    /// <param name="getAllSubjectResources"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<DialogSearchAuthorizationResult> CreateDialogSearchAuthorizationResultFromAuthorizedParties(
        AuthorizedPartiesResult authorizedParties, // Do NOT mutate as this might be a reference to a memory cache
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
}
