using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogTests : ApplicationCollectionFixture
{
    public GetDialogTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Get_Dialog_List_Entity_Ordering()
    {
        // Arrange
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(id: expectedDialogId);

        var createCommandResponse = await Application.Send(createDialogCommand);
        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };

        // Act
        var response = await Application.Send(getDialogQuery);

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        await Verify(result);
    }

    [Fact]
    public async Task Get_Dialog_List_Entity_Ordering_With_Update()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(id: dialogId);

        createDialogCommand.Dto.Content!.Title.Value =
        [
            new LocalizationDto
            {
                LanguageCode = "nb", Value = "Foo"
            }
        ];

        var createCommandResponse = await Application.Send(createDialogCommand);
        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };

        var response = await Application.Send(getDialogQuery);

        response.TryPickT0(out var dialog, out _).Should().BeTrue();

        dialog.Activities.Clear();
        dialog.Transmissions.Clear();

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(dialog);

        updateDialogDto.Content!.Title.Value.Add(new LocalizationDto
        {
            LanguageCode = "en",
            Value = "Bar"
        });

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto,
            IsSilentUpdate = true
        });

        // Assert
        updateResponse.TryPickT0(out _, out _).Should().BeTrue();

        var getDialogAfterUpdateQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getResponse = await Application.Send(getDialogAfterUpdateQuery);
        getResponse.TryPickT0(out var result, out _).Should().BeTrue();

        await Verify(result);
    }

    [Fact]
    public async Task Get_ReturnsSimpleDialog_WhenDialogExists()
    {
        // Arrange
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: expectedDialogId);
        var createCommandResponse = await Application.Send(createDialogCommand);

        // Act
        var response = await Application.Send(new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(createDialogCommand.Dto, options => options
            .Excluding(x => x.UpdatedAt)
            .Excluding(x => x.CreatedAt)
            .Excluding(x => x.SystemLabel));
    }

    [Fact]
    public async Task Get_ReturnsDialog_WhenDialogExists()
    {
        // Arrange
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();
        var createCommand = DialogGenerator.GenerateFakeCreateDialogCommand(id: expectedDialogId);
        var createCommandResponse = await Application.Send(createCommand);

        // Act
        var response = await Application.Send(new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(createCommand.Dto, options => options
            .Excluding(x => x.UpdatedAt)
            .Excluding(x => x.CreatedAt)
            .Excluding(x => x.SystemLabel));
    }
    // TODO: Add tests
}
