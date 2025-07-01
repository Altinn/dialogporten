using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using ContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ContentDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateTransmissionTests : ApplicationCollectionFixture
{
    public UpdateTransmissionTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Cannot_Use_Existing_Attachment_Id_In_Update()
    {
        var existingAttachmentId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                    x.AddAttachment(x => x.Id = existingAttachmentId)))
            .UpdateDialog(x =>
                x.AddTransmission(x =>
                    x.AddAttachment(x => x.Id = existingAttachmentId)))
            .ExecuteAndAssert<DomainError>(error =>
                error.ShouldHaveErrorWithText(existingAttachmentId.ToString()));
    }

    [Fact]
    public Task Can_Create_Simple_Transmission_In_Update() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
            {
                var transmission = UpdateDialogDialogTransmissionDto();
                x.Dto.Transmissions.Add(transmission);
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
                dialog.Transmissions.Count.Should().Be(1));

    [Fact]
    public Task Can_Update_Related_Transmission_With_Null_Id() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
            {
                var transmission = UpdateDialogDialogTransmissionDto();
                var relatedTransmission = UpdateDialogDialogTransmissionDto();

                transmission.RelatedTransmissionId = relatedTransmission.Id;
                transmission.Id = null!;

                x.Dto.Transmissions = [transmission, relatedTransmission];
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
                dialog.Transmissions.Count.Should().Be(2));

    [Fact]
    public Task Can_Add_Transmission_Without_Summary_On_Update() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
                x.AddTransmission(x =>
                    x.Content!.Summary = null))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
                dialog.Transmissions
                    .First().Content.Summary.Should().BeNull());

    [Fact]
    public async Task Cannot_Include_Old_Transmissions_In_UpdateCommand()
    {
        var existingTransmissionId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(count: 1).First();
                transmission.Id = existingTransmissionId;
                x.Dto.Transmissions.Add(transmission);
            })
            .UpdateDialog(x =>
            {
                var transmission = UpdateDialogDialogTransmissionDto();
                transmission.Id = existingTransmissionId;
                x.Dto.Transmissions.Add(transmission);
            })
            .ExecuteAndAssert<DomainError>(error =>
                error.ShouldHaveErrorWithText(existingTransmissionId.ToString()));
    }

    [Fact]
    public Task Cannot_Add_Transmissions_Without_Content_In_IsApiOnlyFalse_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = false)
            .UpdateDialog(x =>
            {
                var newTransmission = UpdateDialogDialogTransmissionDto();
                newTransmission.Content = null!;
                x.Dto.Transmissions.Add(newTransmission);
            })
            .ExecuteAndAssert<ValidationError>(error =>
                error.ShouldHaveErrorWithText(nameof(DialogTransmission.Content)));

    [Fact]
    public Task Can_Add_Transmissions_Without_Content_In_IsApiOnlyFTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .UpdateDialog(x =>
            {
                var newTransmission = UpdateDialogDialogTransmissionDto();
                newTransmission.Content = null!;
                x.Dto.Transmissions.Add(newTransmission);
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog => dialog
                .Transmissions
                .Single()
                .Content
                .Should()
                .BeNull());

    [Fact]
    public Task Should_Validate_Supplied_Transmission_Content_If_IsApiOnlyTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .UpdateDialog(x =>
            {
                var newTransmission = UpdateDialogDialogTransmissionDto();
                newTransmission.Content!.Title = null!;
                x.Dto.Transmissions.Add(newTransmission);
            })
            .ExecuteAndAssert<ValidationError>(error =>
                error.ShouldHaveErrorWithText(nameof(ContentDto.Title)));

    private static TransmissionDto UpdateDialogDialogTransmissionDto() => new()
    {
        Id = IdentifiableExtensions.CreateVersion7(),
        Type = DialogTransmissionType.Values.Information,
        Sender = new() { ActorType = ActorType.Values.ServiceOwner },
        Content = new()
        {
            Title = new() { Value = DialogGenerator.GenerateFakeLocalizations(1) },
            Summary = new() { Value = DialogGenerator.GenerateFakeLocalizations(1) }
        }
    };
}
