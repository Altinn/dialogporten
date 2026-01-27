using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Shouldly;
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
                x.Id.ShouldBe(id);
                x.ExternalReference.ShouldBe(externalReference);
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
                result.Status.ShouldBe(mappedStatus);

                result.ShouldNotBeNull();
                AssertMatchesCreateDialogDto(result, createDto);
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
                x.Transmissions.ShouldNotBeEmpty();
                x.Transmissions.All(transmission =>
                        transmission.Attachments.Count > 0
                        && transmission.Attachments.All(attachment =>
                            attachment.Urls.Count > 0
                            && attachment.Urls.All(url => url.Url is not null && url.Url != Constants.ExpiredUri)))
                    .ShouldBeTrue();

                x.Attachments.ShouldNotBeEmpty();
                x.Attachments.All(attachment =>
                        attachment.Urls.Count > 0
                        && attachment.Urls.All(url => url.Url is not null && url.Url != Constants.ExpiredUri))
                    .ShouldBeTrue();
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
                result.Status.ShouldBe(mappedStatus);

                result.ShouldNotBeNull();
                AssertMatchesCreateDialogDto(result, createDto);
            });
    }

    [Fact]
    [Obsolete("Testing obsolete SystemLabel, will be removed in future versions.")]
    public Task Get_Should_Populate_Obsolete_SystemLabel() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.SystemLabel.ShouldBe(SystemLabel.Values.Default));

    private static void AssertMatchesCreateDialogDto(DialogDto actual, CreateDialogDto expected)
    {
        var excludedProperties = new HashSet<string>
        {
            nameof(CreateDialogDto.UpdatedAt),
            nameof(CreateDialogDto.CreatedAt),
            nameof(CreateDialogDto.SystemLabel),
            nameof(CreateDialogDto.Status)
        };

        var expectedProperties = typeof(CreateDialogDto)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var expectedProperty in expectedProperties)
        {
            if (excludedProperties.Contains(expectedProperty.Name))
            {
                continue;
            }

            var actualProperty = typeof(DialogDto)
                .GetProperty(expectedProperty.Name, BindingFlags.Instance | BindingFlags.Public);

            actualProperty.ShouldNotBeNull();

            var expectedValue = expectedProperty.GetValue(expected);
            var actualValue = actualProperty!.GetValue(actual);

            actualValue.ShouldBeEquivalentTo(expectedValue);
        }
    }
}
