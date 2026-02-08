using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using TransmissionAttachmentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission.TransmissionAttachmentDto;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Constants = Digdir.Domain.Dialogporten.Domain.Common.Constants;

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
}
