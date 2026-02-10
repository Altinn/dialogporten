using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public class IntegrationTestServiceResourceAuthorizer : IServiceResourceAuthorizer
{
    public Task<AuthorizeServiceResourcesResult> AuthorizeServiceResources(DialogEntity dialog,
        CancellationToken cancellationToken) => Task.FromResult<AuthorizeServiceResourcesResult>(new Success());

    public Task<SetResourceTypeResult> SetResourceType(DialogEntity dialog, CancellationToken cancellationToken)
    {
        dialog.ServiceResourceType = "GenericAccessResource";
        return Task.FromResult<SetResourceTypeResult>(new Success());
    }
}
