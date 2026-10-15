using DomainActorType = Digdir.Domain.Dialogporten.Domain.Actors.ActorType;
using DomainDialogStatus = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogStatus;
using DomainActivityType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities.DialogActivityType;
using DomainSystemLabel = Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities.SystemLabel;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

// Explicit enum-to-enum mappers. Each member is mapped by hand (rather than by a generic name/value
// converter) so that only intentionally related enums can be converted and unexpected values fail loudly.
internal static class EnumMapExtensions
{
    public static ActorType ToGraphQl(this DomainActorType.Values source) => source switch
    {
        DomainActorType.Values.PartyRepresentative => ActorType.PartyRepresentative,
        DomainActorType.Values.ServiceOwner => ActorType.ServiceOwner,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    public static DialogStatus ToGraphQl(this DomainDialogStatus.Values source) => source switch
    {
        DomainDialogStatus.Values.NotApplicable => DialogStatus.NotApplicable,
        DomainDialogStatus.Values.InProgress => DialogStatus.InProgress,
        DomainDialogStatus.Values.Draft => DialogStatus.Draft,
        DomainDialogStatus.Values.Awaiting => DialogStatus.Awaiting,
        DomainDialogStatus.Values.RequiresAttention => DialogStatus.RequiresAttention,
        DomainDialogStatus.Values.Completed => DialogStatus.Completed,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    public static DomainDialogStatus.Values ToDomain(this DialogStatus source) => source switch
    {
        DialogStatus.NotApplicable => DomainDialogStatus.Values.NotApplicable,
        DialogStatus.InProgress => DomainDialogStatus.Values.InProgress,
        DialogStatus.Draft => DomainDialogStatus.Values.Draft,
        DialogStatus.Awaiting => DomainDialogStatus.Values.Awaiting,
        DialogStatus.RequiresAttention => DomainDialogStatus.Values.RequiresAttention,
        DialogStatus.Completed => DomainDialogStatus.Values.Completed,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    public static ActivityType ToGraphQl(this DomainActivityType.Values source) => source switch
    {
        DomainActivityType.Values.DialogCreated => ActivityType.DialogCreated,
        DomainActivityType.Values.DialogClosed => ActivityType.DialogClosed,
        DomainActivityType.Values.Information => ActivityType.Information,
        DomainActivityType.Values.TransmissionOpened => ActivityType.TransmissionOpened,
        DomainActivityType.Values.PaymentMade => ActivityType.PaymentMade,
        DomainActivityType.Values.SignatureProvided => ActivityType.SignatureProvided,
        DomainActivityType.Values.DialogOpened => ActivityType.DialogOpened,
        DomainActivityType.Values.DialogDeleted => ActivityType.DialogDeleted,
        DomainActivityType.Values.DialogRestored => ActivityType.DialogRestored,
        DomainActivityType.Values.SentToSigning => ActivityType.SentToSigning,
        DomainActivityType.Values.SentToFormFill => ActivityType.SentToFormFill,
        DomainActivityType.Values.SentToSendIn => ActivityType.SentToSendIn,
        DomainActivityType.Values.SentToPayment => ActivityType.SentToPayment,
        DomainActivityType.Values.FormSubmitted => ActivityType.FormSubmitted,
        DomainActivityType.Values.FormSaved => ActivityType.FormSaved,
        DomainActivityType.Values.CorrespondenceOpened => ActivityType.CorrespondenceOpened,
        DomainActivityType.Values.CorrespondenceConfirmed => ActivityType.CorrespondenceConfirmed,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    public static SystemLabel ToGraphQl(this DomainSystemLabel.Values source) => source switch
    {
        DomainSystemLabel.Values.Default => SystemLabel.Default,
        DomainSystemLabel.Values.Bin => SystemLabel.Bin,
        DomainSystemLabel.Values.Archive => SystemLabel.Archive,
        DomainSystemLabel.Values.MarkedAsUnopened => SystemLabel.MarkedAsUnopened,
        DomainSystemLabel.Values.Sent => SystemLabel.Sent,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    public static DomainSystemLabel.Values ToDomain(this SystemLabel source) => source switch
    {
        SystemLabel.Default => DomainSystemLabel.Values.Default,
        SystemLabel.Bin => DomainSystemLabel.Values.Bin,
        SystemLabel.Archive => DomainSystemLabel.Values.Archive,
        SystemLabel.MarkedAsUnopened => DomainSystemLabel.Values.MarkedAsUnopened,
        SystemLabel.Sent => DomainSystemLabel.Values.Sent,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };
}
