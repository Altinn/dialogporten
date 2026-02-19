using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateTransmissionTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task UpdateTransmission_Returns_ValidationError_When_IsSilentUpdate_Not_Set() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog(x => x.AddTransmission())
            .UpdateTransmission(_ => { })
            .ExecuteAndAssert<ValidationError>(error =>
                error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.IsSilentUpdate)));

    [Fact]
    public Task UpdateTransmission_Returns_Forbidden_Without_ChangeTransmissions_Scope() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.AddTransmission())
            .UpdateTransmission(x => x.IsSilentUpdate = true)
            .ExecuteAndAssert<Forbidden>(error =>
                error.Reasons.Should().ContainSingle(reason =>
                    reason.Contains(AuthorizationScope.ServiceProviderChangeTransmissions)));

    [Fact]
    public Task UpdateTransmission_Returns_NotFound_When_Dialog_Is_Not_Owned() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog(x => x.AddTransmission())
            .UpdateTransmission(x => x.IsSilentUpdate = true)
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>();

    private const string UpdatedExternalReference = "updated-external-reference";

    [Fact]
    public Task UpdateTransmission_Persists_Changes_When_Silent_Update_And_Scope_Are_Present() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateTransmission(_ => { })
            .UpdateTransmission(x =>
            {
                x.IsSilentUpdate = true;
                x.Dto.ExternalReference = UpdatedExternalReference;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
                dialog.Transmissions.Single().ExternalReference.Should().Be(UpdatedExternalReference));
}
