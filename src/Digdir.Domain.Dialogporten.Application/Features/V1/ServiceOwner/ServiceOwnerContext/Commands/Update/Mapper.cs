using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;

internal static class ServiceOwnerContextMapper
{
    extension(ServiceOwnerLabelDto source)
    {
        public DialogServiceOwnerLabel ToEntity() => new()
        {
            Value = source.Value
        };
    }
}
