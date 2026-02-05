using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateTransmissionTests : ApplicationCollectionFixture
{
    public CreateTransmissionTests(DialogApplication application) : base(application) { }

    private const string ParentTransmissionIdKey = nameof(ParentTransmissionIdKey);

    [Fact]
    public Task Can_Create_Transmission_Related_To_Existing_Transmission() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((command, ctx) =>
            {
                var parentTransmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                parentTransmission.Id = parentTransmission.Id.CreateVersion7IfDefault();
                var parentId = parentTransmission.Id!.Value;
                ctx.Bag[ParentTransmissionIdKey] = parentId;
                command.Dto.Transmissions = [parentTransmission];
            })
            .AssertResult<CreateDialogSuccess>()
            .CreateTransmission((transmission, ctx) =>
            {
                var parentId = (Guid)ctx.Bag[ParentTransmissionIdKey]!;
                transmission.RelatedTransmissionId = parentId;
            })
            .AssertResult<CreateTransmissionSuccess>()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
            {
                result.Transmissions.Should().HaveCount(2);
                result.Transmissions.Last().RelatedTransmissionId.Should()
                    .Be(result.Transmissions.First().Id);
            });
}
