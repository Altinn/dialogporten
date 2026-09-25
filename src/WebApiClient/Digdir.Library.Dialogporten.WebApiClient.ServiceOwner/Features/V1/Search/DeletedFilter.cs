using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Search;

public enum DeletedFilter
{
    [EnumMember(Value = @"Exclude")]
    Exclude = 0,

    [EnumMember(Value = @"Include")]
    Include = 1,

    [EnumMember(Value = @"Only")]
    Only = 2,
}
