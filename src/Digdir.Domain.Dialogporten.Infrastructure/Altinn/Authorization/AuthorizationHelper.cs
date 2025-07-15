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
    public static async Task<DialogSearchAuthorizationResult> CollapseSubjectResources(
        AuthorizedPartiesResult authorizedParties, // Do NOT mutate as this might be a reference to a memory cache
        List<string> constraintParties,
        List<string> constraintResources,
        Func<CancellationToken, Task<List<SubjectResource>>> getAllSubjectResources,
        CancellationToken cancellationToken)
    {
        var result = new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, HashSet<string>>(100) // Pre-size with a reasonable capacity
        };

        // Quick check for empty input
        if (authorizedParties.AuthorizedParties.Count == 0)
            return result;

        // Step 1: Pre-filter parties with roles and build the unique subjects set. Skip any parties that are not in the constraints (if supplied)
        var uniqueSubjects = new HashSet<string>(100);
        var partiesWithRolesOrAccessPackages = new List<(string Party, List<string> RolesAndAccessPackages)>();
        var constraintPartiesSet = constraintParties.Count > 0 ? new HashSet<string>(constraintParties) : null;
        var constraintResourcesSet = constraintResources.Count > 0 ? new HashSet<string>(constraintResources) : null;

        foreach (var party in authorizedParties.AuthorizedParties.Where(p => constraintPartiesSet is null || constraintPartiesSet.Contains(p.Party)))
        {
            if (!(party.AuthorizedRolesAndAccessPackages.Count > 0)) continue;
            partiesWithRolesOrAccessPackages.Add((party.Party, party.AuthorizedRolesAndAccessPackages));

            foreach (var role in party.AuthorizedRolesAndAccessPackages)
            {
                uniqueSubjects.Add(role);
            }
        }

        if (partiesWithRolesOrAccessPackages.Count > 0)
        {
            // Step 2: Get and preprocess subject resources
            var subjectResources = await getAllSubjectResources(cancellationToken);

            // Step 3: Build subject-to-resources dictionary with early filtering
            var subjectToResources = new Dictionary<string, HashSet<string>>(uniqueSubjects.Count);
            foreach (var sr in subjectResources)
            {
                // Skip if not in our subjects list
                if (!uniqueSubjects.Contains(sr.Subject))
                    continue;

                // Skip if constraint resources exist and this resource isn't in the constraints
                if (constraintResourcesSet != null && !constraintResourcesSet.Contains(sr.Resource))
                    continue;

                // Add to our lookup dictionary
                if (!subjectToResources.TryGetValue(sr.Subject, out var resources))
                {
                    resources = new HashSet<string>();
                    subjectToResources[sr.Subject] = resources;
                }

                resources.Add(sr.Resource);
            }

            // Step 4: Populate result dictionary with a single pass
            foreach (var (party, roles) in partiesWithRolesOrAccessPackages)
            {
                var partyResources = new HashSet<string>();
                var hasResources = false;

                foreach (var role in roles)
                {
                    if (subjectToResources.TryGetValue(role, out var resources))
                    {
                        partyResources.UnionWith(resources);
                        hasResources = true;
                    }
                }

                if (hasResources)
                {
                    result.ResourcesByParties[party] = partyResources;
                }
            }
        }

        // Step 5: Handle parties that have direct resource authorizations and instance delegations
        foreach (var party in authorizedParties.AuthorizedParties
                     .Where(p => constraintPartiesSet is null || constraintPartiesSet.Contains(p.Party)))
        {
            // We'll only allocate/insert the HashSet if we hit at least one matching resource
            HashSet<string>? existingResources = null;

            foreach (var resource in party.AuthorizedResources)
            {
                if (constraintResourcesSet != null && !constraintResourcesSet.Contains(resource))
                    continue;

                if (existingResources == null)
                {
                    if (!result.ResourcesByParties.TryGetValue(party.Party, out existingResources!))
                    {
                        existingResources = new HashSet<string>();
                        result.ResourcesByParties[party.Party] = existingResources;
                    }
                }

                existingResources.Add(resource);
            }

            foreach (var instance in party.AuthorizedInstances)
            {
                // We currently only support Altinn 3.0 app instance delegations from Altinn Access Management
                if (!instance.ResourceId.StartsWith(Constants.AppResourceIdPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (constraintResourcesSet != null && !constraintResourcesSet.Contains(Constants.ServiceResourcePrefix + instance.ResourceId))
                    continue;

                result.AltinnAppInstanceIds.Add(Constants.ServiceContextInstanceIdPrefix + party.PartyId + "/" + instance.InstanceId);
            }
        }

        return result;
    }
}
