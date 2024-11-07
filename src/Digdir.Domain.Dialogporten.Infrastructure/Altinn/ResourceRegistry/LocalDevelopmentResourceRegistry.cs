﻿using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.ResourcePolicyMetadata;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.ResourceRegistry;

internal sealed class LocalDevelopmentResourceRegistry : IResourceRegistry
{
    private const string LocalResourceType = "LocalResourceType";
    private const string LocalOrgId = "742859274";
    private static readonly HashSet<ServiceResourceInformation> CachedResourceIds = new(new ServiceResourceInformationEqualityComparer());
    private readonly IDialogDbContext _db;

    public LocalDevelopmentResourceRegistry(IDialogDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyCollection<ServiceResourceInformation>> GetResourceInformationForOrg(string orgNumber, CancellationToken cancellationToken)
    {
        var newIds = await _db.Dialogs
            .Select(x => x.ServiceResource)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var id in newIds)
        {
            CachedResourceIds.Add(new ServiceResourceInformation(id, LocalResourceType, orgNumber));
        }

        return CachedResourceIds;
    }

    public Task<ServiceResourceInformation?> GetResourceInformation(string serviceResourceId, CancellationToken cancellationToken)
    {
        return Task.FromResult<ServiceResourceInformation?>(
            new ServiceResourceInformation(serviceResourceId, LocalResourceType, LocalOrgId));
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public IAsyncEnumerable<List<UpdatedSubjectResource>> GetUpdatedSubjectResources(DateTimeOffset _, int __, CancellationToken ___)
        => AsyncEnumerableExtensions.Empty<List<UpdatedSubjectResource>>();

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public Task<IReadOnlyCollection<UpdatedResourcePolicyMetadata>> GetUpdatedResourcePolicyMetadata(DateTimeOffset _, int __, CancellationToken ___)
        => Task.FromResult<IReadOnlyCollection<UpdatedResourcePolicyMetadata>>(Array.Empty<UpdatedResourcePolicyMetadata>());

    private sealed class ServiceResourceInformationEqualityComparer : IEqualityComparer<ServiceResourceInformation>
    {
        public bool Equals(ServiceResourceInformation? x, ServiceResourceInformation? y)
            => x?.ResourceId == y?.ResourceId && x?.OwnerOrgNumber == y?.OwnerOrgNumber;

        public int GetHashCode(ServiceResourceInformation obj)
            => HashCode.Combine(obj.ResourceId, obj.OwnerOrgNumber);
    }
}
