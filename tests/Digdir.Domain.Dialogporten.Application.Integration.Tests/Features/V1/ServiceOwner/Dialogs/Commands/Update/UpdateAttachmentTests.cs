using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Update;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateAttachmentTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Can_Update_Dialog_Attachment_ExpiresAt() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddAttachment(x =>
                    x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(3)))
            .UpdateDialog(x =>
                x.Dto.Attachments.First().ExpiresAt =
                    DateTimeOffset.UtcNow.AddDays(1))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Attachments.Single()
                    .ExpiresAt.Should().NotBeNull()
                    .And.BeAfter(DateTimeOffset.UtcNow)
                    .And.BeBefore(DateTimeOffset.UtcNow.AddDays(2)));

    [Fact]
    public Task Can_Remove_Dialog_Attachment_ExpiresAt() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddAttachment(x =>
                    x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)))
            .UpdateDialog(x =>
                x.Dto.Attachments.First().ExpiresAt = null)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Attachments.Single()
                    .ExpiresAt.Should().BeNull());

    [Fact]
    public Task Can_Update_Dialog_Attachment_Url_With_User_Defined_Id()
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
            .AssertSuccessAndUpdateDialog(x =>
                x.Dto.Attachments.Single().Urls.Single().Url = new Uri("https://example.com/b.pdf"))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var attachmentUrl = x.Attachments.Single().Urls.Should().ContainSingle().Subject;
                attachmentUrl.Id.Should().Be(attachmentUrlId);
                attachmentUrl.Url.Should().Be(new Uri("https://example.com/b.pdf"));
            });
    }

    [Fact]
    public Task Can_Add_New_Attachment_Url_With_User_Defined_Id_To_Existing_Attachment()
    {
        var newUrlId = NewUuidV7();

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.AddAttachment())
            .AssertSuccessAndUpdateDialog(x =>
                x.Dto.Attachments.Single().Urls.Add(new AttachmentUrlDto
                {
                    Id = newUrlId,
                    Url = new Uri("https://example.com/new.pdf"),
                    ConsumerType = AttachmentUrlConsumerType.Values.Gui
                }))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Attachments.Single().Urls.Should().Contain(u => u.Id == newUrlId));
    }

    [Fact]
    public Task Does_Not_Recreate_Dialog_Attachment_Url_When_User_Defined_Id_Is_Unchanged()
    {
        const string CreatedAtKey = "attachment-url-created-at";
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
            .AssertSuccess()
            .Do(async ctx =>
            {
                var attachmentUrl = (await ctx.Application.GetDbEntities<AttachmentUrl>())
                    .Single(x => x.Id == attachmentUrlId);
                ctx.Bag[CreatedAtKey] = attachmentUrl.CreatedAt;
            })
            .UpdateDialog(_ => { })
            .AssertResult<UpdateDialogSuccess>()
            .Do(async ctx =>
            {
                var attachmentUrl = (await ctx.Application.GetDbEntities<AttachmentUrl>())
                    .Single(x => x.Id == attachmentUrlId);
                attachmentUrl.CreatedAt.Should().Be((DateTimeOffset)ctx.Bag[CreatedAtKey]!);
            })
            .UpdateDialog(_ => { })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Attachments.Single().Urls.Should().ContainSingle(x => x.Id == attachmentUrlId));
    }

    [Fact]
    public Task Cannot_Update_Dialog_Attachment_ExpiresAt_To_Past_Date() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddAttachment(x =>
                    x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(2)))
            .UpdateDialog(x =>
                x.Dto.Attachments.First().ExpiresAt =
                    DateTimeOffset.UtcNow.AddDays(-1))
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithPropertyNameText(nameof(AttachmentDto.ExpiresAt)));

    [Fact]
    public Task Cannot_Add_Dialog_Attachment_With_ExpiresAt_In_The_Past() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
                x.AddAttachment(x =>
                    x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)))
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithPropertyNameText(nameof(AttachmentDto.ExpiresAt)));

    [Fact]
    public Task Cannot_Update_Dialog_Attachment_Url_With_Non_UuidV7_Id() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddAttachment(x =>
                {
                    x.Urls =
                    [
                        new()
                        {
                            Url = new Uri("https://example.com/a.pdf"),
                            ConsumerType = AttachmentUrlConsumerType.Values.Gui
                        }
                    ];
                }))
            .AssertSuccessAndUpdateDialog(x =>
                x.Dto.Attachments.Single().Urls.Single().Id = Guid.NewGuid())
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(nameof(AttachmentUrlDto.Id)));

    [Fact]
    public Task Cannot_Update_Dialog_Attachment_With_Duplicate_Url_Ids()
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
            .AssertSuccessAndUpdateDialog(x =>
                x.Dto.Attachments.Single().Urls.Add(new AttachmentUrlDto
                {
                    Id = attachmentUrlId,
                    Url = new Uri("https://example.com/b.pdf"),
                    ConsumerType = AttachmentUrlConsumerType.Values.Api
                }))
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(attachmentUrlId.ToString()));
    }

    [Fact]
    public Task Can_Add_Transmission_Attachment_With_ExpiresAt() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
                x.AddTransmission(x =>
                    x.AddAttachment(x =>
                        x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1))))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Transmissions.Single().Attachments.Single()
                    .ExpiresAt.Should().NotBeNull()
                    .And.BeAfter(DateTimeOffset.UtcNow));

    [Fact]
    public Task Cannot_Add_Transmission_Attachment_With_ExpiresAt_In_The_Past() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
                x.AddTransmission(x =>
                    x.AddAttachment(x =>
                        x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1))))
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithPropertyNameText(nameof(AttachmentDto.ExpiresAt)));
}
