using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;

[SuppressMessage("Style", "IDE0072:Add missing cases")]
internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DialogStatusInput, DialogStatus.Values>()
            .ConvertUsing((src, _) => src switch
            {
                DialogStatusInput.New => DialogStatus.Values.NotApplicable,
                DialogStatusInput.Sent => DialogStatus.Values.Awaiting,
                _ => (DialogStatus.Values)src
            });
    }
}
