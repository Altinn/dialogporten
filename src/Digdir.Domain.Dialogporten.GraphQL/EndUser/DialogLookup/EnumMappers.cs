using IdentifierLookupGrantType = Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup.IdentifierLookupGrantType;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogLookup;

internal static class DialogLookupEnumMapExtensions
{
    public static DialogLookupGrantType ToGraphQl(this IdentifierLookupGrantType source) => source switch
    {
        IdentifierLookupGrantType.Role => DialogLookupGrantType.Role,
        IdentifierLookupGrantType.AccessPackage => DialogLookupGrantType.AccessPackage,
        IdentifierLookupGrantType.ResourceDelegation => DialogLookupGrantType.ResourceDelegation,
        IdentifierLookupGrantType.InstanceDelegation => DialogLookupGrantType.InstanceDelegation,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };
}
