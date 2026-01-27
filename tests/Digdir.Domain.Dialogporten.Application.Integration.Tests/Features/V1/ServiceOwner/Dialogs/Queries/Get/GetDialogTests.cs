using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Get_Should_Return_Dialog_With_Correct_Id()
    {
        const string externalReference = "Bare for å være sikker...";
        var id = NewUuidV7();
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog(x => (x.Dto.Id, x.Dto.ExternalReference) = (id, externalReference))
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .SendCommand(_ => new GetDialogQuery { DialogId = id })
            .ExecuteAndAssert<DialogDto>(x =>
            {
                Assert.Equal(id, x.Id);
                Assert.Equal(externalReference, x.ExternalReference);
            });
    }

    [Fact]
    public async Task Get_ReturnsSimpleDialog_WhenDialogExists()
    {
        CreateDialogDto createDto = null!;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => createDto = x.Dto)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
            {
                var mappedStatus = Application.GetMapper()
                    .Map<DialogStatus.Values>(createDto.Status);
                Assert.Equal(mappedStatus, result.Status);

                Assert.NotNull(result);
                var expected = Application.GetMapper().Map<DialogDto>(createDto);
                expected.UpdatedAt = result.UpdatedAt;
                expected.CreatedAt = result.CreatedAt;
#pragma warning disable CS0618 // DialogDto.SystemLabel is obsolete in favor of EndUserContext.SystemLabels
                expected.SystemLabel = result.SystemLabel;
#pragma warning restore CS0618
                expected.Status = result.Status;
                Assert.Equivalent(expected, result);
            });
    }

    [Fact]
    public Task Get_Dialog_Should_Not_Mask_Expired_Attachment_Urls() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.Now.AddDays(1));
                x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.Now.AddDays(1));

                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
            })
            .OverrideUtc(TimeSpan.FromDays(2))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                Assert.NotEmpty(x.Transmissions);
                Assert.All(x.Transmissions, transmission =>
                {
                    Assert.NotEmpty(transmission.Attachments);
                    Assert.All(transmission.Attachments, attachment =>
                    {
                        Assert.NotEmpty(attachment.Urls);
                        Assert.All(attachment.Urls, url =>
                        {
                            var urlValue = url.Url;
                            Assert.NotNull(urlValue);
                            Assert.NotEqual(Constants.ExpiredUri, urlValue);
                        });
                    });
                });

                Assert.NotEmpty(x.Attachments);
                Assert.All(x.Attachments, attachment =>
                {
                    Assert.NotEmpty(attachment.Urls);
                    Assert.All(attachment.Urls, url =>
                    {
                        var urlValue = url.Url;
                        Assert.NotNull(urlValue);
                        Assert.NotEqual(Constants.ExpiredUri, urlValue);
                    });
                });
            });

    [Fact]
    public async Task Get_ReturnsDialog_WhenDialogExists()
    {
        CreateDialogDto createDto = null!;

        await FlowBuilder.For(Application)
            .CreateComplexDialog(x => createDto = x.Dto)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
            {
                var mappedStatus = Application.GetMapper()
                    .Map<DialogStatus.Values>(createDto.Status);
                Assert.Equal(mappedStatus, result.Status);

                Assert.NotNull(result);
                var expected = Application.GetMapper().Map<DialogDto>(createDto);
                expected.UpdatedAt = result.UpdatedAt;
                expected.CreatedAt = result.CreatedAt;
#pragma warning disable CS0618 // DialogDto.SystemLabel is obsolete in favor of EndUserContext.SystemLabels
                expected.SystemLabel = result.SystemLabel;
#pragma warning restore CS0618
                expected.Status = result.Status;
                Assert.Equivalent(expected, result);
            });
    }

    [Fact]
    [Obsolete("Testing obsolete SystemLabel, will be removed in future versions.")]
    public Task Get_Should_Populate_Obsolete_SystemLabel() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                Assert.Equal(SystemLabel.Values.Default, x.SystemLabel));
}
