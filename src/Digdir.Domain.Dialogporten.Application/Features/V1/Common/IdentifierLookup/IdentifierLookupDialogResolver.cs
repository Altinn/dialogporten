using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Microsoft.EntityFrameworkCore;
using static Digdir.Domain.Dialogporten.Application.Common.ResourceRegistry.Constants;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

/// <summary>
/// Resolves lookup dialog metadata from dialog id or service owner labels.
/// </summary>
internal sealed class IdentifierLookupDialogResolver : IIdentifierLookupDialogResolver
{
    private readonly IDialogDbContext _db;
    private readonly IResourceRegistry _resourceRegistry;

    public IdentifierLookupDialogResolver(IDialogDbContext db, IResourceRegistry resourceRegistry)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(resourceRegistry);
        _db = db;
        _resourceRegistry = resourceRegistry;
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
    public async Task<string> ResolveOutputInstanceRef(InstanceRef requestRef, IdentifierLookupDialogData dialogData,
        CancellationToken cancellationToken)
    {
        if (requestRef.Type is not InstanceRefType.DialogId)
        {
            return requestRef.Value;
        }

        var outputInstanceRef = InstanceRef.FromDialog(dialogData.DialogId, dialogData.ServiceOwnerLabels);
        if (outputInstanceRef.Type is not InstanceRefType.DialogId)
        {
            return outputInstanceRef.Value;
        }

        // TODO: Remove workaround: https://github.com/Altinn/dialogporten/issues/3613
        // Pending https://github.com/Altinn/altinn-correspondence/issues/1777 (and backfill), we use a workaround to
        // find the output instance ref for correspondence dialogs without a correspondence id label.
        if (await IsCorrespondenceService(dialogData.ServiceResource, cancellationToken))
        {
            var fceUrl = await _db.DialogContents
                .Where(x => x.DialogId == dialogData.DialogId &&
                            x.TypeId == DialogContentType.Values.MainContentReference)
                .SelectMany(x => x.Value.Localizations)
                .Select(loc => loc.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (TryExtractCorrespondenceId(fceUrl, out var correspondenceId))
            {
                return AltinnAuthorizationConstants.CorrespondenceRefPrefix + correspondenceId;
            }

            throw new InvalidOperationException("Unable to determine correspondence ID instance reference for correspondence dialog. No label found and FCE URL is not in expected format.");
        }

        return outputInstanceRef.Value;
    }

    private async Task<bool> IsCorrespondenceService(string dialogDataServiceResource, CancellationToken cancellationToken)
    {
        var resourceInformation = await _resourceRegistry.GetResourceInformation(dialogDataServiceResource, cancellationToken);
        return resourceInformation?.ResourceType == CorrespondenceService;
    }

    private const string Prefix = "/correspondence/api/v1/correspondence/";
    private const string Suffix = "/content";
    private static bool TryExtractCorrespondenceId(ReadOnlySpan<char> url, [NotNullWhen(true)] out string? id)
    {
        id = null;

        var schemeSep = url.IndexOf("://".AsSpan());
        if (schemeSep < 0)
            return false;

        var pathStart = url[(schemeSep + 3)..].IndexOf('/');
        if (pathStart < 0)
            return false;

        pathStart += schemeSep + 3;

        var path = url[pathStart..];

        if (!path.StartsWith(Prefix.AsSpan(), StringComparison.Ordinal))
            return false;

        if (!path.EndsWith(Suffix.AsSpan(), StringComparison.Ordinal))
            return false;

        var guidStart = Prefix.Length;
        var guidLength = path.Length - Prefix.Length - Suffix.Length;

        if (guidLength != 36)
            return false;

        id = path.Slice(guidStart, guidLength).ToString();

        return true;
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
        CancellationToken cancellationToken) =>
        await _db.DialogServiceOwnerLabels
            .Where(l => l.Value == labelValue)
            .Join(dialogs,
                l => l.DialogServiceOwnerContextId,
                d => d.Id, (l, d) => d.Id)
            .FirstOrDefaultAsync(cancellationToken);

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
