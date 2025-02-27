using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events;

internal static class CloudEventTypes
{
    internal static string Get(string eventName) => eventName switch
    {
        // Dialog
        nameof(DialogRestoredDomainEvent) => "dialogporten.dialog.restored.v1",
        nameof(DialogCreatedDomainEvent) => "dialogporten.dialog.created.v1",
        nameof(DialogUpdatedDomainEvent) => "dialogporten.dialog.updated.v1",
        nameof(DialogDeletedDomainEvent) => "dialogporten.dialog.deleted.v1",
        nameof(DialogSeenDomainEvent) => "dialogporten.dialog.seen.v1",
        nameof(DialogTransmissionCreatedDomainEvent) => "dialogporten.dialog.transmission.created.v1",

        // Dialog activity
        nameof(DialogActivityType.Values.DialogCreated) => "dialogporten.dialog.activity.created.v1",
        nameof(DialogActivityType.Values.DialogClosed) => "dialogporten.dialog.activity.closed.v1",
        nameof(DialogActivityType.Values.Information) => "dialogporten.dialog.activity.information.v1",
        nameof(DialogActivityType.Values.TransmissionOpened) => "dialogporten.dialog.activity.transmission-opened.v1",
        nameof(DialogActivityType.Values.PaymentMade) => "dialogporten.dialog.activity.payment-made.v1",
        nameof(DialogActivityType.Values.SignatureProvided) => "dialogporten.dialog.activity.signature-provided.v1",
        nameof(DialogActivityType.Values.DialogOpened) => "dialogporten.dialog.activity.dialog-opened.v1",
        nameof(DialogActivityType.Values.DialogDeleted) => "dialogporten.dialog.activity.dialog-deleted.v1",
        nameof(DialogActivityType.Values.DialogRestored) => "dialogporten.dialog.activity.dialog-restored.v1",
        nameof(DialogActivityType.Values.SentToSigning) => "dialogporten.dialog.activity.sent-to-signing.v1",
        nameof(DialogActivityType.Values.SentToFormFill) => "dialogporten.dialog.activity.sent-to-form-fill.v1",
        nameof(DialogActivityType.Values.SentToSendIn) => "dialogporten.dialog.activity.sent-to-send-in.v1",
        nameof(DialogActivityType.Values.SentToPayment) => "dialogporten.dialog.activity.sent-to-payment.v1",
        nameof(DialogActivityType.Values.FormSubmitted) => "dialogporten.dialog.activity.form-submitted.v1",
        nameof(DialogActivityType.Values.FormSaved) => "dialogporten.dialog.activity.form-saved.v1",

        _ => throw new ArgumentOutOfRangeException(nameof(eventName), eventName, null)
    };
}
