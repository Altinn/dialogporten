using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using TransmissionAttachmentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission.TransmissionAttachmentDto;
using GetTransmissionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.GetTransmission.TransmissionDto;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Constants = Digdir.Domain.Dialogporten.Domain.Common.Constants;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

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

    private const string TransmissionAttachmentName = "transmission-attachment";

    [Fact]
    public Task Can_Create_Transmission_With_Attachment_Name() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateTransmission((x, _) => x.AddAttachment(x => x.Name = TransmissionAttachmentName))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
                result.Transmissions.Last()
                    .Attachments.Should()
                    .ContainSingle(attachment =>
                        attachment.Name == TransmissionAttachmentName));

    [Fact]
    public Task Cannot_Create_Transmission_With_Name_Longer_Than_DefaultMaxLength() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateTransmission((x, _) =>
                x.AddAttachment(attachment =>
                    attachment.Name = new string('a', Constants.DefaultMaxStringLength + 1)))
            .ExecuteAndAssert<ValidationError>(result =>
                result.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Name)));

    [Fact]
    public Task VisibleFrom_Should_Control_Timestamps_On_Create()
    {
        var visibleFrom = DateTimeOffset.UtcNow.AddDays(3);
        var transmissionId = NewUuidV7();

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.VisibleFrom = visibleFrom)
            .CreateTransmission((x, _) => x.Id = transmissionId)
            .GetServiceOwnerTransmission(transmissionId)
            .ExecuteAndAssert<GetTransmissionDto>(transmission =>
                transmission.CreatedAt
                    .Should()
                    .BeCloseTo(visibleFrom, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public Task Can_Not_Create_Transmission_On_Deleted_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .CreateTransmission((_, _) => { })
            .ExecuteAndAssert<EntityDeleted<DialogEntity>>((x, ctx) =>
            {
                x.Name.Should().Be("DialogEntity");
                var id = ctx.GetDialogId();
                x.Message.Should().Be($"Entity 'DialogEntity' with the following key(s) is removed: ({id}).");
            });

    [Fact]
    public Task Cannot_Create_More_Than_ShortMaxValue_Transmissions() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertResult<CreateDialogSuccess>()
            .Modify((_, ctx) =>
            {
                using var scope = Application.GetServiceProvider().CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
                var dialogEntity = dbContext.Dialogs
                    .Single(x => x.Id == ctx.GetDialogId());
                dialogEntity.FromServiceOwnerTransmissionsCount = short.MaxValue - 1;
                dbContext.SaveChanges();
            })
            .CreateTransmission((_, _) => { })
            .AssertResult<CreateTransmissionSuccess>()
            .CreateTransmission((_, _) => { })
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText($"cannot exceed {short.MaxValue}"));

    [Fact]
    public Task Cannot_Create_Transmission_Url_With_Media_Type_Exceeding_Max_Length() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateTransmission((x, _) =>
                x.AddAttachment(x =>
                    x.Urls.First().MediaType = new string('a', TestConstants.DefaultMaxStringLength + 1)))
            .ExecuteAndAssert<ValidationError>();
}
