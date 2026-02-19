using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using TransmissionAttachmentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.TransmissionAttachmentDto;
using TransmissionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.TransmissionDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UniqueConstraintTests : ApplicationCollectionFixture
{
    public UniqueConstraintTests(DialogApplication application) : base(application) { }

    # region Create

    private sealed class ExistingDbIdTestData : TheoryData<string, Action<CreateDialogCommand, FlowContext>, string>
    {
        public ExistingDbIdTestData()
        {
            var dialogId = NewUuidV7();
            Add("Cannot create dialog with existing id",
                (x, _) => x.Dto.Id = dialogId,
                dialogId.ToString());

            var dialogAttachment = DialogGenerator.GenerateFakeDialogAttachment();
            Add("Cannot create dialog attachment with existing attachment id",
                (x, _) => x.Dto.Attachments.Add(dialogAttachment),
                dialogAttachment.Id.ToString()!);

            var dialogActivity = DialogGenerator.GenerateFakeDialogActivity();
            Add("Cannot create dialog activity with existing id",
                (x, _) => x.Dto.Activities.Add(dialogActivity),
                dialogActivity.Id.ToString()!);

            var guiAction = DialogGenerator.GenerateFakeDialogGuiActions()[0];
            Add("Cannot create dialog gui action with existing id",
                (x, _) => x.Dto.GuiActions.Add(guiAction),
                guiAction.Id.ToString()!);

            var apiAction = DialogGenerator.GenerateFakeDialogApiActions()[0];
            Add("Cannot create dialog api action with existing id",
                (x, _) => x.Dto.ApiActions.Add(apiAction),
                apiAction.Id.ToString()!);

            var dialogTransmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
            Add("Cannot create dialog transmission with existing id",
                (x, _) => x.Dto.Transmissions.Add(dialogTransmission),
                dialogTransmission.Id.ToString()!);

            var transmissionAttachment = new TransmissionAttachmentDto
            {
                Id = NewUuidV7(),
                DisplayName = DialogGenerator.GenerateFakeLocalizations(1),
                Urls =
                [
                    new()
                    {
                        ConsumerType = AttachmentUrlConsumerType.Values.Api,
                        Url = new Uri("https://example.com")
                    }
                ]
            };
            Add("Cannot create transmission attachment with existing id",
                (x, _) =>
                {
                    var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                    transmission.Attachments.Add(transmissionAttachment);
                    x.Dto.Transmissions.Add(transmission);
                },
                transmissionAttachment.Id.ToString()!);
        }
    }

    [Theory, ClassData(typeof(ExistingDbIdTestData))]
    public Task Existing_Database_Ids_Tests(string _,
        Action<CreateDialogCommand, FlowContext> createDialogCommand, string conflictingId) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(createDialogCommand)
            .CreateSimpleDialog(createDialogCommand)
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText(conflictingId));

    [Fact]
    public async Task Cannot_Use_Existing_IdempotentKey_When_Creating_Dialog()
    {
        var idempotentKey = NewUuidV7().ToString();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.IdempotentKey = idempotentKey)
            .CreateSimpleDialog((x, _) => x.Dto.IdempotentKey = idempotentKey)
            .ExecuteAndAssert<Conflict>(x =>
                x.ErrorMessage.Should().Contain(idempotentKey));
    }

    [Fact]
    public Task Cannot_Exceed_Dialog_IdempotentKey_Max_Length_When_Creating_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x
                .Dto.IdempotentKey = "Random string which is longer than maximum allowed idempotentKey length")
            .ExecuteAndAssert<ValidationError>(x =>
            {
                var err = x.Errors.Single();
                err.ErrorCode.Should().Be("MaximumLengthValidator");
                err.ErrorMessage.Should().Contain(nameof(CreateDialogDto.IdempotentKey));
            });

    [Fact]
    public Task Cannot_Go_Below_The_Dialog_IdempotentKey_Min_Length_When_Creating_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x
                    .Dto.IdempotentKey = "be")
            .ExecuteAndAssert<ValidationError>(x =>
            {
                var err = x.Errors.Single();
                err.ErrorCode.Should().Be("MinimumLengthValidator");
                err.ErrorMessage.Should().Contain(nameof(CreateDialogDto.IdempotentKey));
            });

    [Fact]
    public async Task Cannot_Use_Duplicate_Transmission_IdempotentKey_When_Creating_Dialog()
    {
        var idempotentKey1 = NewUuidV7().ToString();
        var idempotentKey2 = NewUuidV7().ToString();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x
                .AddTransmission(x =>
                    x.IdempotentKey = idempotentKey1)
                .AddTransmission(x =>
                    x.IdempotentKey = idempotentKey1)
                .AddTransmission(x =>
                    x.IdempotentKey = idempotentKey2)
                .AddTransmission(x =>
                    x.IdempotentKey = idempotentKey2))
            .ExecuteAndAssert<Conflict>(x => x
                .ErrorMessage.Should().ContainAll(idempotentKey1, idempotentKey2));
    }

    [Fact]
    public Task Cannot_Exceed_Transmission_IdempotentKey_Max_Length_When_Creating_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x
                .AddTransmission(x =>
                    x.IdempotentKey = "Random string which is longer than maximum allowed idempotentKey length"))
            .ExecuteAndAssert<ValidationError>(x =>
            {
                var err = x.Errors.Single();
                err.ErrorCode.Should().Be("MaximumLengthValidator");
                err.ErrorMessage.Should().Contain(nameof(TransmissionDto.IdempotentKey));
            });

    [Fact]
    public Task Cannot_Go_Below_The_Transmission_IdempotentKey_Min_Length_When_Creating_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x
                .AddTransmission(x =>
                    x.IdempotentKey = "be"))
            .ExecuteAndAssert<ValidationError>(x =>
            {
                var err = x.Errors.Single();
                err.ErrorCode.Should().Be("MinimumLengthValidator");
                err.ErrorMessage.Should().Contain(nameof(TransmissionDto.IdempotentKey));
            });

    #endregion

    # region Update

    [Fact]
    public async Task Cannot_Append_Transmission_With_Existing_Id()
    {
        var originalTransmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Transmissions.Add(originalTransmission))
            .AssertSuccessAndUpdateDialog(x =>
            {
                var transmission = new TransmissionDto
                {
                    Type = DialogTransmissionType.Values.Information,
                    Id = originalTransmission.Id,
                    Sender = new() { ActorType = ActorType.Values.ServiceOwner },
                    Content = new()
                    {
                        Summary = new() { Value = DialogGenerator.GenerateFakeLocalizations(1) },
                        Title = new() { Value = DialogGenerator.GenerateFakeLocalizations(1) }
                    }
                };
                x.Dto.Transmissions.Add(transmission);
            })
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText(originalTransmission.Id.ToString()!));
    }

    [Fact]
    public async Task Cannot_Use_Existing_Transmission_IdempotentKey_When_Updating_Dialog()
    {
        var idempotentKey1 = NewUuidV7().ToString();
        var idempotentKey2 = NewUuidV7().ToString();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x
                .AddTransmission(x =>
                    x.IdempotentKey = idempotentKey1)
                .AddTransmission(x =>
                    x.IdempotentKey = idempotentKey2))
            .UpdateDialog(x =>
                x.AddTransmission(x => x.IdempotentKey = idempotentKey1)
                    .AddTransmission(x => x.IdempotentKey = idempotentKey2))
            .ExecuteAndAssert<Conflict>(x => x
                .ErrorMessage.Should().ContainAll(idempotentKey1, idempotentKey2));
    }

    [Fact]
    public async Task Cannot_Use_Duplicate_Transmission_IdempotentKey_When_Updating_Dialog()
    {
        var idempotentKey1 = NewUuidV7().ToString();
        var idempotentKey2 = NewUuidV7().ToString();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x => x
                .AddTransmission(t => t.IdempotentKey = idempotentKey1)
                .AddTransmission(t => t.IdempotentKey = idempotentKey1)
                .AddTransmission(t => t.IdempotentKey = idempotentKey2)
                .AddTransmission(t => t.IdempotentKey = idempotentKey2))
            .ExecuteAndAssert<Conflict>(x => x
                .ErrorMessage.Should().ContainAll(idempotentKey1, idempotentKey2));
    }

    [Fact]
    public async Task Cannot_Exceed_Transmission_IdempotentKey_Max_Length_When_Updating_Dialog()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x
                .AddTransmission())
            .UpdateDialog(x => x.AddTransmission(x =>
                x.IdempotentKey = "Random string which is longer than maximum allowed idempotentKey length"))
            .ExecuteAndAssert<ValidationError>(x =>
            {
                var err = x.Errors.Single();
                err.ErrorCode.Should().Be("MaximumLengthValidator");
                err.ErrorMessage.Should().Contain(nameof(TransmissionDto.IdempotentKey));
            });
    }

    [Fact]
    public async Task Cannot_Go_Below_The_Transmission_IdempotentKey_Min_Length_When_Updating_Dialog()
    {
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x
                .AddTransmission())
            .UpdateDialog(x => x.AddTransmission(x =>
                x.IdempotentKey = "be"))
            .ExecuteAndAssert<ValidationError>(x =>
            {
                var err = x.Errors.Single();
                err.ErrorCode.Should().Be("MinimumLengthValidator");
                err.ErrorMessage.Should().Contain(nameof(TransmissionDto.IdempotentKey));
            });
    }

    [Fact]
    public async Task Cannot_Append_Activity_With_Existing_Id()
    {
        var dialogActivity = DialogGenerator.GenerateFakeDialogActivities(1)[0];

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.Activities.Add(dialogActivity))
            .AssertSuccessAndUpdateDialog(x =>
            {
                x.Dto.Activities.Add(new()
                {
                    Id = dialogActivity.Id,
                    PerformedBy = new() { ActorType = ActorType.Values.ServiceOwner },
                    Type = DialogActivityType.Values.DialogClosed
                });
            })
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText(dialogActivity.Id.ToString()!));
    }

    #endregion
}
