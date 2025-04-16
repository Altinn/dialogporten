using Digdir.Library.Entity.Abstractions.Features.Lookup;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

public sealed class DialogActivityType : AbstractLookupEntity<DialogActivityType, DialogActivityType.Values>
{
    public DialogActivityType(Values id) : base(id) { }
    public override DialogActivityType MapValue(Values id) => new(id);

    public enum Values
    {
        /// <summary>
        /// Indicates that a dialog has been created.
        /// </summary>
        DialogCreated = 1,

        /// <summary>
        /// Indicates that a dialog has been closed.
        /// </summary>
        DialogClosed = 2,

        /// <summary>
        /// Information from the service provider, not (directly) related to any transmission.
        /// </summary>
        Information = 3,

        /// <summary>
        /// Indicates that a transmission has been opened.
        /// </summary>
        TransmissionOpened = 4,

        /// <summary>
        /// Indicates that payment has been made.
        /// </summary>
        PaymentMade = 5,

        /// <summary>
        /// Indicates that a signature has been provided.
        /// </summary>
        SignatureProvided = 6,

        /// <summary>
        /// Indicates that a dialog has been opened.
        /// </summary>
        DialogOpened = 7,

        /// <summary>
        /// Indicates that a dialog has been deleted.
        /// </summary>
        DialogDeleted = 8,

        /// <summary>
        /// Indicates that a dialog has been restored.
        /// </summary>
        DialogRestored = 9,

        /// <summary>
        /// Indicates that a dialog has been sent to signing.
        /// </summary>
        SentToSigning = 10,

        /// <summary>
        /// Indicates that a dialog has been sent to form fill.
        /// </summary>
        SentToFormFill = 11,

        /// <summary>
        /// Indicates that a dialog has been sent to send in.
        /// </summary>
        SentToSendIn = 12,

        /// <summary>
        /// Indicates that a dialog has been sent to payment.
        /// </summary>
        SentToPayment = 13,

        /// <summary>
        /// Indicates that a form associated with the dialog has been submitted.
        /// </summary>
        FormSubmitted = 14,

        /// <summary>
        /// Indicates that a form associated with the dialog has been saved.
        /// </summary>
        FormSaved = 15,

        /// <summary>
        /// Indicates that a correspondence has been opened.
        /// </summary>
        CorrespondenceOpened = 16,

        /// <summary>
        /// Indicates that a correspondence has been confirmed.
        /// </summary>
        CorrespondenceConfirmed = 17,
    }
}
