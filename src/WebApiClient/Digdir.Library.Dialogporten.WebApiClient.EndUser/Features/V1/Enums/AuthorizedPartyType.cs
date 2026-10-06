using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

public enum AuthorizedPartyType
{
    [EnumMember(Value = @"Organization")]
    Organization = 1,

    [EnumMember(Value = @"Person")]
    Person = 2
}
