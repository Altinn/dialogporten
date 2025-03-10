using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Digdir.Tool.Dialogporten.Benchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CollapseSubjectResourcesBenchmark
{
    // Parameters for controlling the size of inputs
    [Params(20, 1000)]
    public int PartyCount { get; set; }

    [Params(5, 20)]
    public int RolesPerParty { get; set; }

    [Params(0)]
    public int ConstraintResourcesCount { get; set; }

    [Params(100, 1000)]
    public int SubjectResourcesCount { get; set; }

    // Test data
    private AuthorizedPartiesResult _authorizedParties = new();
    private List<string> _constraintResources = [];
    private List<SubjectResource> _allSubjectResources = [];

    [GlobalSetup]
    public void Setup()
    {
        // Generate test data based on parameters
        GenerateTestData();
    }

    private void GenerateTestData()
    {
        // Generate authorized parties
        var parties = new List<AuthorizedParty>();
        for (var i = 0; i < PartyCount; i++)
        {
            var roles = new List<string>();
            for (var j = 0; j < RolesPerParty; j++)
            {
                roles.Add($"role_{i}_{j}");
            }

            parties.Add(new AuthorizedParty
            {
                Party = $"party_{i}",
                AuthorizedRoles = roles
            });
        }
        _authorizedParties = new AuthorizedPartiesResult
        {
            AuthorizedParties = parties
        };

        // Generate constraint resources
        _constraintResources = new List<string>();
        for (var i = 0; i < ConstraintResourcesCount; i++)
        {
            _constraintResources.Add($"resource_{i}");
        }

        // Generate all subject resources
        _allSubjectResources = new List<SubjectResource>();
        var uniqueRoles = parties.SelectMany(p => p.AuthorizedRoles).Distinct().ToList();
        var resourcesPerRole = Math.Max(1, SubjectResourcesCount / uniqueRoles.Count);

        foreach (var role in uniqueRoles)
        {
            for (var i = 0; i < resourcesPerRole; i++)
            {
                _allSubjectResources.Add(new SubjectResource
                {
                    Subject = role,
                    Resource = $"resource_{i}"
                });
            }
        }

        // Add some random resources to reach the desired count
        while (_allSubjectResources.Count < SubjectResourcesCount)
        {
            var randomRole = uniqueRoles[new Random().Next(uniqueRoles.Count)];
            var resourceId = _allSubjectResources.Count;
            _allSubjectResources.Add(new SubjectResource
            {
                Subject = randomRole,
                Resource = $"resource_{resourceId}"
            });
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Implementation")]
    public async Task<DialogSearchAuthorizationResult> Original()
    {
        var dialogSearchAuthorizationResult = new DialogSearchAuthorizationResult
        {
            ResourcesByParties = _authorizedParties.AuthorizedParties
                .ToDictionary(
                    p => p.Party,
                    p => p.AuthorizedResources
                        .Where(r => _constraintResources.Count == 0 || _constraintResources.Contains(r))
                        .ToHashSet())
                // Skip parties with no authorized resources
                .Where(kv => kv.Value.Count != 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value),
        };

        await CollapseSubjectResources(
            dialogSearchAuthorizationResult,
            _authorizedParties,
            _constraintResources,
            _ => Task.FromResult(_allSubjectResources),
            CancellationToken.None);

        return dialogSearchAuthorizationResult;
    }

    // New implementation not mutating input
    [Benchmark]
    [BenchmarkCategory("Implementation")]
    public async Task<DialogSearchAuthorizationResult> NewNonMutating()
    {
        return await CollapseSubjectResources(
            _authorizedParties,
            _constraintResources,
            _ => Task.FromResult(_allSubjectResources),
            CancellationToken.None);
    }

    // Original implementation (mutating input)
    private static async Task CollapseSubjectResources(
        DialogSearchAuthorizationResult dialogSearchAuthorizationResult,
        AuthorizedPartiesResult authorizedParties,
        List<string> constraintResources,
        Func<CancellationToken, Task<List<SubjectResource>>> getAllSubjectResources,
        CancellationToken cancellationToken)
    {
        var authorizedPartiesWithRoles = authorizedParties.AuthorizedParties
            .Where(p => p.AuthorizedRoles.Count != 0)
            .ToList();

        var uniqueSubjects = authorizedPartiesWithRoles
            .SelectMany(p => p.AuthorizedRoles)
            .ToHashSet();

        var subjectResources = (await getAllSubjectResources(cancellationToken))
            .Where(x => uniqueSubjects.Contains(x.Subject) && (constraintResources.Count == 0 || constraintResources.Contains(x.Resource))).ToList();

        var subjectToResources = subjectResources
            .GroupBy(sr => sr.Subject)
            .ToDictionary(g => g.Key, g => g.Select(sr => sr.Resource).ToHashSet());

        foreach (var partyEntry in authorizedPartiesWithRoles)
        {
            if (!dialogSearchAuthorizationResult.ResourcesByParties.TryGetValue(partyEntry.Party, out var resourceList))
            {
                resourceList = new HashSet<string>();
                dialogSearchAuthorizationResult.ResourcesByParties[partyEntry.Party] = resourceList;
            }

            foreach (var subject in partyEntry.AuthorizedRoles)
            {
                if (subjectToResources.TryGetValue(subject, out var subjectResourceSet))
                {
                    resourceList.UnionWith(subjectResourceSet);
                }
            }

            if (resourceList.Count == 0)
            {
                dialogSearchAuthorizationResult.ResourcesByParties.Remove(partyEntry.Party);
            }
        }
    }


    // New implementation
    public static async Task<DialogSearchAuthorizationResult> CollapseSubjectResources(
        AuthorizedPartiesResult authorizedParties,
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

        // Step 1: Pre-filter parties with roles and build the unique subjects set
        var uniqueSubjects = new HashSet<string>(100);
        var partiesWithRoles = new List<(string Party, List<string> Roles)>();

        foreach (var party in authorizedParties.AuthorizedParties)
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

// Mock classes for completeness
public class DialogSearchAuthorizationResult
{
    public Dictionary<string, HashSet<string>> ResourcesByParties { get; init; } = [];
}

public class AuthorizedPartiesResult
{
    public List<AuthorizedParty> AuthorizedParties { get; set; } = [];
}

public class AuthorizedParty
{
    public string Party { get; set; } = null!;
    public List<string> AuthorizedRoles { get; set; } = [];
    public List<string> AuthorizedResources { get; set; } = [];
}

public class SubjectResource
{
    public string Subject { get; set; } = null!;
    public string Resource { get; set; } = null!;
}
