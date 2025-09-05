using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Freeze;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public sealed class FreezeDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Freeze_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.ServiceResource = "urn:altinn:resource:SuperKulTest")
            .SendCommand((_, ctx) => new FreezeDialogCommand
            {
                Id = ctx.GetDialogId()
            })
            .ConfigureServices(x =>
            {
                x.RemoveAll<IServiceResourceAuthorizer>();
                x.AddSingleton<IServiceResourceAuthorizer, TestServiceResourceAuthorizer>();
                x.Decorate<IUserResourceRegistry, TestUserResourceRegistry>();
            })
            .UpdateDialog(x => x.Dto.Progress = 98)
            .ExecuteAndAssert<Forbidden>();
}

internal sealed class TestUserResourceRegistry(IUserResourceRegistry userResourceRegistry) : IUserResourceRegistry
{
    public Task<bool> CurrentUserIsOwner(string serviceResource, CancellationToken cancellationToken) => userResourceRegistry.CurrentUserIsOwner(serviceResource, cancellationToken);
    public Task<IReadOnlyCollection<string>> GetCurrentUserResourceIds(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<string>>(["urn:altinn:resource:SuperKulTest"]);
    public bool UserCanModifyResourceType(string serviceResourceType) => userResourceRegistry.UserCanModifyResourceType(serviceResourceType);
    public bool IsCurrentUserServiceOwnerAdmin() => false;
}

internal sealed class TestServiceResourceAuthorizer : IServiceResourceAuthorizer
{
    public Task<AuthorizeServiceResourcesResult> AuthorizeServiceResources(DialogEntity dialog, CancellationToken cancellationToken) => Task.FromResult<AuthorizeServiceResourcesResult>(new Success());
    public Task<SetResourceTypeResult> SetResourceType(DialogEntity dialog, CancellationToken cancellationToken)
    {
        dialog.ServiceResourceType = "GenericAccessResource";
        return Task.FromResult<SetResourceTypeResult>(new Success());
    }
}
