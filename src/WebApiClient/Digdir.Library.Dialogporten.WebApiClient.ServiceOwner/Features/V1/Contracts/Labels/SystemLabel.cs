namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Labels;

public enum SystemLabel
{
    [System.Runtime.Serialization.EnumMember(Value = @"Default")]
    Default = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Bin")]
    Bin = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"Archive")]
    Archive = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"MarkedAsUnopened")]
    MarkedAsUnopened = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"Sent")]
    Sent = 4,
}