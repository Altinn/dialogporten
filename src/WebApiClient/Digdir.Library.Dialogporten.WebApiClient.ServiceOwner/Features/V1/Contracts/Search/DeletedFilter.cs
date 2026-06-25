namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Search;

public enum DeletedFilter
{
    [System.Runtime.Serialization.EnumMember(Value = @"Exclude")]
    Exclude = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Include")]
    Include = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"Only")]
    Only = 2,
}