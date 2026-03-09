using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Queries.GetServiceOwnerLabels;

internal static class ServiceOwnerLabelMapper
{
    extension(DialogServiceOwnerLabel source)
    {
        public ServiceOwnerLabelDto ToDto() => new()
        {
            Value = source.Value
        };
    }
}
