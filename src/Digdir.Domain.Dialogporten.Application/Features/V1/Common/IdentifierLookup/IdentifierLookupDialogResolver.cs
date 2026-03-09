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
        InstanceUrn urn,
        IdentifierLookupDeletedDialogVisibility deletedDialogVisibility,
        CancellationToken cancellationToken)
    {
        var dialogs = GetDialogQuery(deletedDialogVisibility);

        var dialogId = urn.Type is InstanceUrnType.DialogId
            ? urn.Id
            : await ResolveDialogIdFromLabel(dialogs, urn.Value, cancellationToken);

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

        return new IdentifierLookupDialogData(
            projection.DialogId,
            projection.Party,
            projection.Org,
            projection.ServiceResource,
            projection.ServiceOwnerLabels,
            projection.Title,
            projection.NonSensitiveTitle.Count > 0 ? projection.NonSensitiveTitle : null);
    }

    // For a given request URN and resolved dialog data, determine the most appropriate instance URN to return in the response,
    // preferring app instance URNs, then correspondence URNs, then dialog URNs. There should not be multiple app instance URNs
    // or correspondence URNs for a given dialog, but if there are, prefer the one that is last in ordinal descending order (newest).
    public string ResolveOutputInstanceUrn(InstanceUrn requestUrn, IdentifierLookupDialogData dialogData)
    {
        if (requestUrn.Type is not InstanceUrnType.DialogId)
        {
            return requestUrn.Value;
        }

        var appInstanceUrn = dialogData.ServiceOwnerLabels
            .Select(TryToAppInstanceUrn)
            .Where(x => x is not null)
            .Select(x => x!)
            .OrderByDescending(x => x, StringComparer.Ordinal)
            .FirstOrDefault();

        if (appInstanceUrn is not null)
        {
            return appInstanceUrn;
        }

        var correspondenceUrn = dialogData.ServiceOwnerLabels
            .Where(x => x.StartsWith(InstanceUrn.CorrespondencePrefix, StringComparison.Ordinal))
            .OrderByDescending(x => x, StringComparer.Ordinal)
            .FirstOrDefault();

        if (correspondenceUrn is not null)
        {
            return correspondenceUrn;
        }

        return InstanceUrn.CreateDialogUrn(dialogData.DialogId).ToLowerInvariant();
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

    private static string? TryToAppInstanceUrn(string labelValue)
    {
        if (labelValue.StartsWith(InstanceUrn.AppInstancePrefix, StringComparison.Ordinal))
        {
            return labelValue;
        }

        if (!labelValue.StartsWith(Constants.ServiceContextInstanceIdPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var separator = labelValue.LastIndexOf('/');
        if (separator < 0 || separator == labelValue.Length - 1)
        {
            return null;
        }

        var instanceId = labelValue[(separator + 1)..];
        return Guid.TryParse(instanceId, out _)
            ? (InstanceUrn.AppInstancePrefix + instanceId).ToLowerInvariant()
            : null;
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
