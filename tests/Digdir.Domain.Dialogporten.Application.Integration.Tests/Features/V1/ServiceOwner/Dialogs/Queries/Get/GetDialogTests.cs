using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using FluentAssertions;
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
                x.Id.Should().Be(id);
                x.ExternalReference.Should().Be(externalReference);
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
                result.Status.Should().Be(mappedStatus);

                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(createDto, options => options
                    .Excluding(x => x.UpdatedAt)
                    .Excluding(x => x.CreatedAt)
                    .Excluding(x => x.SystemLabel)
                    .Excluding(x => x.Status)
                );
            });
    }

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
                result.Status.Should().Be(mappedStatus);

                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(createDto, options => options
                    .Excluding(x => x.UpdatedAt)
                    .Excluding(x => x.CreatedAt)
                    .Excluding(x => x.SystemLabel)
                    .Excluding(x => x.Status));
            });
    }

    [Fact]
    [Obsolete("Testing obsolete SystemLabel, will be removed in future versions.")]
    public Task Get_Should_Populate_Obsolete_SystemLabel() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.SystemLabel.Should()
                    .Be(SystemLabel.Values.Default));
}
