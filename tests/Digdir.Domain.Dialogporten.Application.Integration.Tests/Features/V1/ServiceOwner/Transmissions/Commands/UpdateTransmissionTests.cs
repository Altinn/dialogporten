using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Microsoft.Extensions.DependencyInjection;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateTransmissionTests : ApplicationCollectionFixture
{
    public UpdateTransmissionTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task UpdateTransmission_Returns_ValidationError_When_IsSilentUpdate_Not_Set() =>
        FlowBuilder.For(Application, ConfigureUserWithScope(AuthorizationScope.ServiceProviderChangeTransmissions))
            .CreateSimpleDialog(x => x.AddTransmission())
            .UpdateTransmission(x => x.IsSilentUpdate = false)
            .ExecuteAndAssert<ValidationError>(error =>
                error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.IsSilentUpdate)));

    [Fact]
    public Task UpdateTransmission_Returns_Forbidden_Without_ChangeTransmissions_Scope() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.AddTransmission())
            .ConfigureServices(x => x.Decorate<IUserResourceRegistry, MissingChangeTransmissionScopeUserResourceRegistryDecorator>())
            .UpdateTransmission(x => x.IsSilentUpdate = true)
            .ExecuteAndAssert<Forbidden>(error =>
                error.Reasons.Should().ContainSingle(reason =>
                    reason.Contains(AuthorizationScope.ServiceProviderChangeTransmissions)));

    [Fact]
    public Task UpdateTransmission_Returns_NotFound_When_Dialog_Is_Not_Owned() =>
        FlowBuilder.For(Application, ConfigureUserWithScope(AuthorizationScope.ServiceProviderChangeTransmissions))
            .CreateSimpleDialog(x => x.AddTransmission())
            .ConfigureServices(x => x.Decorate<IUserResourceRegistry, NonOwnerUserResourceRegistryDecorator>())
            .UpdateTransmission(x => x.IsSilentUpdate = true)
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>();

    private const string UpdatedExternalReference = "updated-external-reference";

    [Fact]
    public Task UpdateTransmission_Persists_Changes_When_Silent_Update_And_Scope_Are_Present() =>
        FlowBuilder.For(Application, ConfigureUserWithScope(AuthorizationScope.ServiceProviderChangeTransmissions))
            .CreateTransmission(_ => { })
            .UpdateTransmission(x =>
            {
                x.IsSilentUpdate = true;
                x.Dto.ExternalReference = UpdatedExternalReference;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
                dialog.Transmissions.Single().ExternalReference.Should().Be(UpdatedExternalReference));

    private sealed class NonOwnerUserResourceRegistryDecorator(IUserResourceRegistry userResourceRegistry) : IUserResourceRegistry
    {
        public Task<bool> CurrentUserIsOwner(string serviceResource, CancellationToken cancellationToken) =>
            userResourceRegistry.CurrentUserIsOwner(serviceResource, cancellationToken);

        public Task<IReadOnlyCollection<string>> GetCurrentUserResourceIds(CancellationToken cancellationToken) =>
            userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        public bool UserCanModifyResourceType(string serviceResourceType) =>
            userResourceRegistry.UserCanModifyResourceType(serviceResourceType);

        public bool IsCurrentUserServiceOwnerAdmin() => false;

        public bool CurrentUserCanChangeTransmissions() =>
            userResourceRegistry.CurrentUserCanChangeTransmissions();

        public Task<string> GetCurrentUserOrgShortName(CancellationToken cancellationToken) =>
            Task.FromResult("non-owner-org");
    }

    private sealed class MissingChangeTransmissionScopeUserResourceRegistryDecorator(IUserResourceRegistry userResourceRegistry) : IUserResourceRegistry
    {
        public Task<bool> CurrentUserIsOwner(string serviceResource, CancellationToken cancellationToken) =>
            userResourceRegistry.CurrentUserIsOwner(serviceResource, cancellationToken);

        public Task<IReadOnlyCollection<string>> GetCurrentUserResourceIds(CancellationToken cancellationToken) =>
            userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

        public bool UserCanModifyResourceType(string serviceResourceType) =>
            userResourceRegistry.UserCanModifyResourceType(serviceResourceType);

        public bool IsCurrentUserServiceOwnerAdmin() =>
            userResourceRegistry.IsCurrentUserServiceOwnerAdmin();

        public bool CurrentUserCanChangeTransmissions() => false;

        public Task<string> GetCurrentUserOrgShortName(CancellationToken cancellationToken) =>
            userResourceRegistry.GetCurrentUserOrgShortName(cancellationToken);
    }
}
