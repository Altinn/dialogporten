using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

/// <summary>
/// Resolves lookup dialog metadata from dialog id or service owner labels.
/// </summary>
internal sealed class IdentifierLookupDialogResolver : IIdentifierLookupDialogResolver
{
    private readonly IDialogDbContext _db;

    public IdentifierLookupDialogResolver(IDialogDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <summary>
    /// Resolves lookup data for a reference, with configurable deleted-dialog visibility.
    /// </summary>
    public async Task<IdentifierLookupDialogData?> Resolve(
        InstanceRef instanceRef,
        IdentifierLookupDeletedDialogVisibility deletedDialogVisibility,
        CancellationToken cancellationToken)
    {
        var dialogs = GetDialogQuery(deletedDialogVisibility);

        var dialogId = instanceRef.Type is InstanceRefType.DialogId
            ? instanceRef.Id
            : await ResolveDialogIdFromLabel(dialogs, instanceRef.ToLookupLabel(), cancellationToken);

        if (dialogId == Guid.Empty)
        {
            return null;
        }

        var projection = await dialogs
            .Where(x => x.Id == dialogId)
            .Select(x => new IdentifierLookupDialogProjection
            {
                DialogId = x.Id,
                Party = x.Party,
                Org = x.Org,
                ServiceResource = x.ServiceResource,
                ServiceOwnerLabels = x.ServiceOwnerContext.ServiceOwnerLabels
                    .Select(l => l.Value)
                    .ToList(),
                Title = x.Content
                    .Where(c => c.TypeId == DialogContentType.Values.Title)
                    .SelectMany(c => c.Value.Localizations
                        .Select(l => new ResourceLocalization(l.LanguageCode, l.Value)))
                    .ToList(),
                NonSensitiveTitle = x.Content
                    .Where(c => c.TypeId == DialogContentType.Values.NonSensitiveTitle)
                    .SelectMany(c => c.Value.Localizations
                        .Select(l => new ResourceLocalization(l.LanguageCode, l.Value)))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (projection is null)
        {
            return null;
        }

        var nonSensitiveTitle = projection.NonSensitiveTitle.Count > 0
            ? projection.NonSensitiveTitle.ToArray()
            : null;

        return new IdentifierLookupDialogData(
            projection.DialogId,
            projection.Party,
            projection.Org,
            projection.ServiceResource,
            projection.ServiceOwnerLabels.ToArray(),
            projection.Title.ToArray(),
            nonSensitiveTitle);
    }

    /// <summary>
    /// Chooses the instance reference to return, preferring app-instance, then correspondence, then dialog reference.
    /// </summary>
    public string ResolveOutputInstanceRef(InstanceRef requestRef, IdentifierLookupDialogData dialogData)
    {
        if (requestRef.Type is not InstanceRefType.DialogId)
        {
            return requestRef.Value;
        }

        var outputInstanceRef = InstanceRef.FromDialog(dialogData.DialogId, dialogData.ServiceOwnerLabels);
        return outputInstanceRef.Value;
    }

    private IQueryable<DialogEntity> GetDialogQuery(
        IdentifierLookupDeletedDialogVisibility deletedDialogVisibility)
    {
        var dialogs = _db.Dialogs.AsNoTracking();
        if (deletedDialogVisibility is IdentifierLookupDeletedDialogVisibility.IncludeDeleted)
        {
            dialogs = dialogs.IgnoreQueryFilters();
        }

        return dialogs;
    }

    private async Task<Guid> ResolveDialogIdFromLabel(
        IQueryable<DialogEntity> dialogs,
        string labelValue,
        CancellationToken cancellationToken)
    {
        var matches = await _db.DialogServiceOwnerLabels
            .Where(l => l.Value == labelValue)
            .Join(dialogs,
                l => l.DialogServiceOwnerContextId,
                d => d.Id,
                (l, d) => d.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        return matches.Count > 1
            ? throw new InvalidOperationException(
                $"Multiple dialogs found with service owner label '{labelValue}'. " +
                $"Label values must be unique across dialogs.")
            : matches.FirstOrDefault();
    }

    private sealed class IdentifierLookupDialogProjection
    {
        public Guid DialogId { get; init; }
        public string Party { get; init; } = null!;
        public string Org { get; init; } = null!;
        public string ServiceResource { get; init; } = null!;
        public List<string> ServiceOwnerLabels { get; init; } = [];
        public List<ResourceLocalization> Title { get; init; } = [];
        public List<ResourceLocalization> NonSensitiveTitle { get; init; } = [];
    }
}
