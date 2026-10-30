using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum DialogActivityType
{
    [EnumMember(Value = @"DialogCreated")]
    DialogCreated = 1,

    [EnumMember(Value = @"DialogClosed")]
    DialogClosed = 2,

    [EnumMember(Value = @"Information")]
    Information = 3,

    [EnumMember(Value = @"TransmissionOpened")]
    TransmissionOpened = 4,

    [EnumMember(Value = @"PaymentMade")]
    PaymentMade = 5,

    [EnumMember(Value = @"SignatureProvided")]
    SignatureProvided = 6,

    [EnumMember(Value = @"DialogOpened")]
    DialogOpened = 7,

    [EnumMember(Value = @"DialogDeleted")]
    DialogDeleted = 8,

    [EnumMember(Value = @"DialogRestored")]
    DialogRestored = 9,

    [EnumMember(Value = @"SentToSigning")]
    SentToSigning = 10,

    [EnumMember(Value = @"SentToFormFill")]
    SentToFormFill = 11,

    [EnumMember(Value = @"SentToSendIn")]
    SentToSendIn = 12,

    [EnumMember(Value = @"SentToPayment")]
    SentToPayment = 13,

    [EnumMember(Value = @"FormSubmitted")]
    FormSubmitted = 14,

    [EnumMember(Value = @"FormSaved")]
    FormSaved = 15,

    [EnumMember(Value = @"CorrespondenceOpened")]
    CorrespondenceOpened = 16,

    [EnumMember(Value = @"CorrespondenceConfirmed")]
    CorrespondenceConfirmed = 17,
}
