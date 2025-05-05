using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.SubjectResources;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal static class AuthorizationHelper
{
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
        var partiesWithRoles = new List<(string Party, List<string> Roles)>();

        foreach (var party in authorizedParties.AuthorizedParties.Where(p => constraintParties.Count == 0 || constraintParties.Contains(p.Party)))
        {
            if (!(party.AuthorizedRoles.Count > 0)) continue;
            partiesWithRoles.Add((party.Party, party.AuthorizedRoles));

            foreach (var role in party.AuthorizedRoles)
            {
                uniqueSubjects.Add(role);
            }
        }

        if (partiesWithRoles.Count == 0)
            return result;

        // Step 2: Get and preprocess subject resources
        var subjectResources = await getAllSubjectResources(cancellationToken);

        HashSet<string>? constraintResourcesSet = null;
        if (constraintResources.Count > 0)
        {
            constraintResourcesSet = new HashSet<string>(constraintResources);
        }

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
        foreach (var (party, roles) in partiesWithRoles)
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

        return result;
    }
}
