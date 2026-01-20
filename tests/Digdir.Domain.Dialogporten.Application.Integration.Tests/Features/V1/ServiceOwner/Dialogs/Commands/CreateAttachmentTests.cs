using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateAttachmentTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Can_Create_Dialog_Attachment_With_ExpiryDate() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
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
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                    x.AddAttachment(x =>
                        x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1))))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Transmissions.Single().Attachments.Single()
                    .ExpiresAt.Should().NotBeNull()
                    .And.BeAfter(DateTimeOffset.UtcNow));

    [Fact]
    public Task Cannot_Create_Dialog_Attachment_With_Past_ExpiryDate() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddAttachment(x =>
                    x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1)))
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(nameof(AttachmentDto.ExpiresAt)));

    [Fact]
    public Task Cannot_Create_Transmission_Attachment_With_ExpiryDate_In_The_Past() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                    x.AddAttachment(x =>
                        x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1))))
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(nameof(AttachmentDto.ExpiresAt)));
}
