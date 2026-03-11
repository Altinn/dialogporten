using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogLookup;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<EndUserIdentifierLookupDto, DialogLookup>();
        CreateMap<IdentifierLookupServiceResourceDto, DialogLookupServiceResource>();
        CreateMap<IdentifierLookupServiceOwnerDto, DialogLookupServiceOwner>();
        CreateMap<IdentifierLookupAuthorizationEvidenceDto, DialogLookupAuthorizationEvidence>();
        CreateMap<IdentifierLookupAuthorizationEvidenceItemDto, DialogLookupAuthorizationEvidenceItem>();
        CreateMap<IdentifierLookupGrantType, DialogLookupGrantType>();
    }
}
