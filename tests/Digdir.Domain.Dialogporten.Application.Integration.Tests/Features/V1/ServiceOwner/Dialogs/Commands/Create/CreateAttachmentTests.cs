using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Create;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateAttachmentTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Can_Create_Dialog_Attachment_With_ExpiryDate() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddAttachment(x =>
                    x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Attachments.Single()
                    .ExpiresAt.Should().NotBeNull()
                    .And.BeAfter(DateTimeOffset.UtcNow));

    [Fact]
    public Task Can_Create_Transmission_Attachment_With_ExpiryDate() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(x =>
                    x.AddAttachment(x =>
                        x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1))))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Transmissions.Single().Attachments.Single()
                    .ExpiresAt.Should().NotBeNull()
                    .And.BeAfter(DateTimeOffset.UtcNow));

    [Fact]
    public Task Can_Create_Dialog_Attachment_Url_With_User_Defined_Id()
    {
        var attachmentUrlId = NewUuidV7();

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddAttachment(x =>
                {
                    x.Urls =
                    [
                        new()
                        {
                            Id = attachmentUrlId,
                            Url = new Uri("https://example.com/a.pdf"),
                            ConsumerType = AttachmentUrlConsumerType.Values.Gui
                        }
                    ];
                }))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Attachments
                    .SelectMany(x => x.Urls)
                    .Should()
                    .ContainSingle(x => x.Id == attachmentUrlId));
    }

    [Fact]
    public Task Cannot_Create_Dialog_Attachment_With_Past_ExpiryDate() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddAttachment(x =>
                    x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)))
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(nameof(AttachmentDto.ExpiresAt)));

    [Fact]
    public Task Cannot_Create_Dialog_Attachment_Url_With_Non_UuidV7_Id() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddAttachment(x =>
                {
                    x.Urls =
                    [
                        new()
                        {
                            Id = Guid.NewGuid(),
                            Url = new Uri("https://example.com/a.pdf"),
                            ConsumerType = AttachmentUrlConsumerType.Values.Gui
                        }
                    ];
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(nameof(AttachmentUrlDto.Id)));

    [Fact]
    public Task Cannot_Create_Dialog_Attachment_With_Duplicate_Url_Ids()
    {
        var attachmentUrlId = NewUuidV7();

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddAttachment(x =>
                {
                    x.Urls =
                    [
                        new()
                        {
                            Id = attachmentUrlId,
                            Url = new Uri("https://example.com/a.pdf"),
                            ConsumerType = AttachmentUrlConsumerType.Values.Gui
                        },
                        new()
                        {
                            Id = attachmentUrlId,
                            Url = new Uri("https://example.com/b.pdf"),
                            ConsumerType = AttachmentUrlConsumerType.Values.Api
                        }
                    ];
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(attachmentUrlId.ToString()));
    }

    [Fact]
    public Task Cannot_Create_Transmission_Attachment_With_ExpiryDate_In_The_Past() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(x =>
                    x.AddAttachment(x =>
                        x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1))))
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(nameof(AttachmentDto.ExpiresAt)));
}
