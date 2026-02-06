using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
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

    [Fact]
    public async Task Cannot_Create_More_Than_ShortMaxValue_Transmissions()
    {
        var dialog = await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .ExecuteAndAssert<CreateDialogSuccess>();

        foreach (var batch in Enumerable.Range(0, short.MaxValue).Chunk(100))
        {
            await FlowBuilder.For(Application)
            .SendCommand(_ =>
                {
                    var transmissions = batch
                        .Select(_ => CreateTransmissionDto())
                        .ToArray();

                    var command = new CreateTransmissionCommand
                    {
                        IsSilentUpdate = true,
                        DialogId = dialog.DialogId,
                        Transmissions = [.. transmissions]
                    };
                    return command;
                })
                .ExecuteAndAssert<CreateTransmissionSuccess>();
        }


        await FlowBuilder.For(Application)
            // .SendCommand(_ => new GetDialogQuery { DialogId = dialog.DialogId })
            // .AssertResult<DialogDto>(result => result.Transmissions.Count.Should().Be(short.MaxValue))
            .SendCommand(_ =>
            {
                var command = new CreateTransmissionCommand
                {
                    IsSilentUpdate = true,
                    DialogId = dialog.DialogId,
                    Transmissions = [CreateTransmissionDto()]
                };
                return command;
            }).ExecuteAndAssert<DomainError>(x => x.ShouldHaveErrorWithText($"cannot exceed {short.MaxValue}"));
    }

    private static CreateTransmissionDto CreateTransmissionDto()
    {
        return new CreateTransmissionDto
        {
            Type = DialogTransmissionType.Values.Information,
            Sender = new()
            {
                ActorType = ActorType.Values.ServiceOwner
            },
            Content = new()
            {
                Title = new ContentValueDto
                {
                    Value =
                    [
                        new LocalizationDto
                        {
                            LanguageCode = "nb",
                            Value = "Ny melding"
                        }
                    ]
                }
            }
        };
    }
}
