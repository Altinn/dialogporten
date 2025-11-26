using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateAttachmentTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Can_Update_Dialog_Attachment_ExpiresAt() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
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
            .CreateSimpleDialog(x =>
                x.AddAttachment(x =>
                    x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)))
            .UpdateDialog(x =>
                x.Dto.Attachments.First().ExpiresAt = null)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Attachments.Single()
                    .ExpiresAt.Should().BeNull());

    [Fact]
    public Task Cannot_Update_Dialog_Attachment_ExpiresAt_To_Past_Date() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
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
