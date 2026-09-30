using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogLookup;

internal static class DialogLookupMapExtensions
{
    public static DialogLookup ToDialogLookup(this EndUserIdentifierLookupDto source) => new()
    {
        DialogId = source.DialogId,
        InstanceRef = source.InstanceRef,
        Party = source.Party,
        Title = source.Title.ToGraphQlLocalizations(),
        ServiceResource = ToServiceResource(source.ServiceResource),
        ServiceOwner = ToServiceOwner(source.ServiceOwner),
        AuthorizationEvidence = ToAuthorizationEvidence(source.AuthorizationEvidence)
    };

    internal static DialogLookupServiceResource ToServiceResource(IdentifierLookupServiceResourceDto source) => new()
    {
        Id = source.Id,
        IsDelegable = source.IsDelegable,
        MinimumAuthenticationLevel = source.MinimumAuthenticationLevel,
        Name = source.Name.ToGraphQlLocalizations()
    };

    internal static DialogLookupServiceOwner ToServiceOwner(IdentifierLookupServiceOwnerDto source) => new()
    {
        OrgNumber = source.OrgNumber,
        Code = source.Code,
        Name = source.Name.ToGraphQlLocalizations()
    };

    internal static DialogLookupAuthorizationEvidence ToAuthorizationEvidence(IdentifierLookupAuthorizationEvidenceDto source) => new()
    {
        CurrentAuthenticationLevel = source.CurrentAuthenticationLevel,
        ViaRole = source.ViaRole,
        ViaAccessPackage = source.ViaAccessPackage,
        ViaResourceDelegation = source.ViaResourceDelegation,
        ViaInstanceDelegation = source.ViaInstanceDelegation,
        Evidence = source.Evidence.Select(ToEvidenceItem).ToList()
    };

    internal static DialogLookupAuthorizationEvidenceItem ToEvidenceItem(IdentifierLookupAuthorizationEvidenceItemDto source) => new()
    {
        GrantType = source.GrantType.MapByName<DialogLookupGrantType>(),
        Subject = source.Subject,
        Name = source.Name.ToGraphQlLocalizations(),
        Links = source.Links is null ? null : ToLinks(source.Links)
    };

    internal static DialogLookupLinks ToLinks(LinkDto source) => new()
    {
        Metadata = source.Metadata
    };
}
