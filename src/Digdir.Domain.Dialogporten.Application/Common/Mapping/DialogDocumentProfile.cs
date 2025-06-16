using AutoMapper;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Documents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Common.Mapping;

internal sealed class DialogDocumentProfile : Profile
{
    public DialogDocumentProfile()
    {
        CreateMap<DialogEntity, DialogDocument>()
            .ForMember(d => d.DialogData, o => o.MapFrom(s => s));
    }
}
