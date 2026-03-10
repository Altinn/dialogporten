using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

internal sealed class IdentifierLookupDialogResolver : IIdentifierLookupDialogResolver
{
    private readonly IDialogDbContext _db;

    public IdentifierLookupDialogResolver(IDialogDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IdentifierLookupDialogData?> Resolve(
        InstanceRef instanceRef,
        IdentifierLookupDeletedDialogVisibility deletedDialogVisibility,
        CancellationToken cancellationToken)
    {
        var dialogs = GetDialogQuery(deletedDialogVisibility);

        var dialogId = instanceRef.Type is InstanceRefType.DialogId
            ? instanceRef.Id
            : await ResolveDialogIdFromLabel(dialogs, CreateLookupLabel(instanceRef), cancellationToken);

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

        return projection is null
            ? null
            : new IdentifierLookupDialogData(
            projection.DialogId,
            projection.Party,
            projection.Org,
            projection.ServiceResource,
            projection.ServiceOwnerLabels,
            projection.Title,
            projection.NonSensitiveTitle.Count > 0 ? projection.NonSensitiveTitle : null);
    }

    // For a given request reference and resolved dialog data, determine the most appropriate instance reference to return
    // in the response, preferring app-instance references, then correspondence references, then dialog references.
    // There should not be multiple app-instance or correspondence references for a given dialog, but if there are,
    // prefer the one that is last in ordinal descending order (newest).
    public string ResolveOutputInstanceRef(InstanceRef requestRef, IdentifierLookupDialogData dialogData)
    {
        if (requestRef.Type is not InstanceRefType.DialogId)
        {
            return requestRef.Value;
        }

        var appInstanceRef = dialogData.ServiceOwnerLabels
            .Select(x => TryToAppInstanceRef(x, out var appInstanceLabel) ? appInstanceLabel : null)
            .OfType<string>()
            .OrderByDescending(x => x, StringComparer.Ordinal)
            .FirstOrDefault();

        if (appInstanceRef is not null)
        {
            return appInstanceRef;
        }

        var correspondenceRef = dialogData.ServiceOwnerLabels
            .Where(x => x.StartsWith(InstanceRef.CorrespondencePrefix, StringComparison.Ordinal))
            .OrderByDescending(x => x, StringComparer.Ordinal)
            .FirstOrDefault();

        return correspondenceRef ?? InstanceRef.CreateDialogRef(dialogData.DialogId).ToLowerInvariant();
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

    private static async Task<Guid> ResolveDialogIdFromLabel(
        IQueryable<DialogEntity> dialogs,
        string labelValue,
        CancellationToken cancellationToken) =>
        await dialogs
            .Where(x => x.ServiceOwnerContext.ServiceOwnerLabels.Any(l => l.Value == labelValue))
            .OrderByDescending(x => x.Id)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static string CreateLookupLabel(InstanceRef instanceRef) =>
        instanceRef.Type is not InstanceRefType.AppInstanceId || instanceRef.PartyId is null
            ? instanceRef.Value
            : $"{Constants.ServiceContextInstanceIdPrefix}{instanceRef.PartyId.Value}/{instanceRef.Id}"
                .ToLowerInvariant();

    private static bool TryToAppInstanceRef(string labelValue, [NotNullWhen(true)] out string? appInstanceRef)
    {
        appInstanceRef = null;

        if (labelValue.StartsWith(InstanceRef.AppInstancePrefix, StringComparison.Ordinal))
        {
            appInstanceRef = labelValue.ToLowerInvariant();
            return true;
        }

        if (!labelValue.StartsWith(Constants.ServiceContextInstanceIdPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var separator = labelValue.LastIndexOf('/');
        if (separator < 0 || separator == labelValue.Length - 1)
        {
            return false;
        }

        var appInstanceSuffix = labelValue[Constants.ServiceContextInstanceIdPrefix.Length..];
        appInstanceRef = $"{InstanceRef.AppInstancePrefix}{appInstanceSuffix}".ToLowerInvariant();
        return true;
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
