using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum DialogActivityType
{
    [EnumMember(Value = @"DialogCreated")]
    DialogCreated = 0,

    [EnumMember(Value = @"DialogClosed")]
    DialogClosed = 1,

    [EnumMember(Value = @"Information")]
    Information = 2,

    [EnumMember(Value = @"TransmissionOpened")]
    TransmissionOpened = 3,

    [EnumMember(Value = @"PaymentMade")]
    PaymentMade = 4,

    [EnumMember(Value = @"SignatureProvided")]
    SignatureProvided = 5,

    [EnumMember(Value = @"DialogOpened")]
    DialogOpened = 6,

    [EnumMember(Value = @"DialogDeleted")]
    DialogDeleted = 7,

    [EnumMember(Value = @"DialogRestored")]
    DialogRestored = 8,

    [EnumMember(Value = @"SentToSigning")]
    SentToSigning = 9,

    [EnumMember(Value = @"SentToFormFill")]
    SentToFormFill = 10,

    [EnumMember(Value = @"SentToSendIn")]
    SentToSendIn = 11,

    [EnumMember(Value = @"SentToPayment")]
    SentToPayment = 12,

    [EnumMember(Value = @"FormSubmitted")]
    FormSubmitted = 13,

    [EnumMember(Value = @"FormSaved")]
    FormSaved = 14,

    [EnumMember(Value = @"CorrespondenceOpened")]
    CorrespondenceOpened = 15,

    [EnumMember(Value = @"CorrespondenceConfirmed")]
    CorrespondenceConfirmed = 16,
}
