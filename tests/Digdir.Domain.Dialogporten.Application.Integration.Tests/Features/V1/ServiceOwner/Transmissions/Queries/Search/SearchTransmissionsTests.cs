using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchTransmissions;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Shouldly;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchTransmissionsTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Search_Transmission_Should_Include_ExternalReference() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x => x.ExternalReference = "ext"))
            .SendCommand((_, ctx) => new SearchTransmissionQuery
            {
                DialogId = ctx.GetDialogId(),
            })
            .ExecuteAndAssert<List<TransmissionDto>>(x =>
            {
                var transmission = x.Single();
                transmission.ExternalReference.ShouldBe("ext");
            });

    [Fact]
    public Task Search_Transmission_Should_Not_Mask_Expired_Attachment_Urls() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
            })
            .OverrideUtc(TimeSpan.FromDays(2))
            .SendCommand((_, ctx) => new SearchTransmissionQuery
            {
                DialogId = ctx.GetDialogId(),
            })
            .ExecuteAndAssert<List<TransmissionDto>>(x =>
            {
                x.ShouldNotBeEmpty();
                x.All(transmission =>
                        transmission.Attachments.Count > 0
                        && transmission.Attachments.All(attachment => attachment.ExpiresAt is not null)
                        && transmission.Attachments.All(attachment =>
                            attachment.Urls.Count > 0
                            && attachment.Urls.All(url => url.Url != Constants.ExpiredUri)))
                    .ShouldBeTrue();
            });
}
