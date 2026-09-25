using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using StrawberryShake;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Features.LabelAssignmentLog;

[Collection(nameof(GraphQlTestCollectionFixture))]
public class LabelAssignmentLogTests(GraphQlE2EFixture fixture) : E2ETestBase<GraphQlE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_Label_Assignment_Log_After_Setting_Label()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        var setLabelResult = await Fixture.GraphQlClient.SetSystemLabel.ExecuteAsync(
            new SetSystemLabelInput
            {
                DialogId = dialogId,
                AddLabels = [SystemLabel.Bin],
                RemoveLabels = []
            },
            TestContext.Current.CancellationToken);

        setLabelResult.Errors.Should().BeEmpty();
        setLabelResult.Data.Should().NotBeNull();
        setLabelResult.Data.SetSystemLabel.Success.Should().BeTrue();

        // Act
        var result = await GetLabelAssignmentLog(dialogId);

        // Assert
        result.Data.Should().NotBeNull();

        var payload = result.Data.LabelAssignmentLog;
        payload.Errors.Should().BeEmpty();
        payload.LabelAssignmentLog.Should().ContainSingle()
            .Which.Action.Should().Be("set");
    }

    [E2EFact]
    public async Task Should_Return_Typed_NotFound_Error_For_Unknown_DialogId()
    {
        // Arrange
        var dialogId = Guid.NewGuid();

        // Act
        var result = await GetLabelAssignmentLog(dialogId);

        // Assert
        result.Data.Should().NotBeNull();

        var error = result.Data.LabelAssignmentLog.Errors.Single();

        error.Should().BeOfType<GetLabelAssignmentLog_LabelAssignmentLog_Errors_LabelAssignmentLogNotFound>();
        error.Message.Should().Contain(dialogId.ToString());
    }

    private Task<IOperationResult<IGetLabelAssignmentLogResult>> GetLabelAssignmentLog(Guid dialogId) =>
        Fixture.GraphQlClient.GetLabelAssignmentLog.ExecuteAsync(dialogId, TestContext.Current.CancellationToken);
}
