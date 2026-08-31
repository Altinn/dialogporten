using System.Runtime.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

public enum AuthorizationContextUnauthorizedPresentation
{
    [EnumMember(Value = @"Disabled")]
    Disabled = 0,

    [EnumMember(Value = @"Excluded")]
    Excluded = 1,
}
