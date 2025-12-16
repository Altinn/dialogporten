using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common;

internal interface IDialogTransmissionAppender
{
    DialogTransmissionAppendResult Append(
        DialogEntity dialog,
        IReadOnlyCollection<DialogTransmission> newTransmissions);
}

internal sealed record DialogTransmissionAppendResult(
    bool ContainsEndUserTransmission,
    bool ContainsServiceOwnerTransmission);

internal sealed class DialogTransmissionAppender : IDialogTransmissionAppender
{
    private readonly IDialogDbContext _db;
    private readonly IClock _clock;
    private readonly IDomainContext _domainContext;

    public DialogTransmissionAppender(
        IDialogDbContext db,
        IClock clock,
        IDomainContext domainContext)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
    }

    public DialogTransmissionAppendResult Append(
        DialogEntity dialog,
        IReadOnlyCollection<DialogTransmission> newTransmissions)
    {
        if (newTransmissions.Count == 0)
        {
            return new DialogTransmissionAppendResult(false, false);
        }

        var (fromParty, fromServiceOwner) = newTransmissions.GetTransmissionCounts();

        dialog.FromPartyTransmissionsCount = checked((short)(dialog.FromPartyTransmissionsCount + fromParty));
        dialog.FromServiceOwnerTransmissionsCount = checked((short)(dialog.FromServiceOwnerTransmissionsCount + fromServiceOwner));

        dialog.Transmissions.AddRange(newTransmissions);
        _db.DialogTransmissions.AddRange(newTransmissions);

        foreach (var attachment in newTransmissions.SelectMany(x => x.Attachments))
        {
            ValidateTimeFields(attachment);
        }

        return new DialogTransmissionAppendResult(
            fromParty > 0,
            fromServiceOwner > 0);
    }

    private void ValidateTimeFields(DialogTransmissionAttachment attachment)
    {
        if (!_db.MustWhenAdded(attachment,
                propertyExpression: x => x.ExpiresAt,
                predicate: x => x > _clock.UtcNowOffset || x == null))
        {
            var idString = attachment.Id == Guid.Empty ? string.Empty : $" (Id: {attachment.Id})";

            _domainContext.AddError($"{nameof(DialogEntity.Transmissions)}." +
                                    $"{nameof(DialogTransmission.Attachments)}." +
                                    $"{nameof(DialogTransmissionAttachment.ExpiresAt)}",
                $"Must be in future or null, got '{attachment.ExpiresAt}'.{idString}");
        }
    }
}
