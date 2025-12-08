using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Freeze;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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


    [Fact]
    public async Task FreezeDialogCommand_Should_Return_New_Revision()
    {
        Guid? originalRevision = null;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.ServiceResource = "urn:altinn:resource:SuperKulTest")
            .AssertResult<CreateDialogSuccess>(x =>
            {
                x.Revision.Should().NotBeEmpty();
                originalRevision = x.Revision;
            })
            .SendCommand((_, ctx) => new FreezeDialogCommand
            {
                Id = ctx.GetDialogId()
            })
            .ExecuteAndAssert<FreezeDialogSuccess>(x =>
            {
                x.Revision.Should().NotBeEmpty();
                x.Revision.Should().NotBe(originalRevision!.Value);
            });
    }
}

internal sealed class TestUserResourceRegistry(IUserResourceRegistry userResourceRegistry) : IUserResourceRegistry
{
    public Task<bool> CurrentUserIsOwner(string serviceResource, CancellationToken cancellationToken) => userResourceRegistry.CurrentUserIsOwner(serviceResource, cancellationToken);
    public Task<IReadOnlyCollection<string>> GetCurrentUserResourceIds(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<string>>(["urn:altinn:resource:SuperKulTest"]);
    public bool UserCanModifyResourceType(string serviceResourceType) => userResourceRegistry.UserCanModifyResourceType(serviceResourceType);
    public bool IsCurrentUserServiceOwnerAdmin() => false;
    public Task<IReadOnlyCollection<string>> GetCurrentUserOrgShortNames(CancellationToken cancellationToken) => throw new NotImplementedException();
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
