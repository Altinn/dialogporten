using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests.Mapping;

public class DialogStatusMappingTests
{
    [Theory]
    [InlineData(DialogStatus.InProgress, DialogStatusInput.InProgress)]
    [InlineData(DialogStatus.Draft, DialogStatusInput.Draft)]
    [InlineData(DialogStatus.RequiresAttention, DialogStatusInput.RequiresAttention)]
    [InlineData(DialogStatus.Completed, DialogStatusInput.Completed)]
    [InlineData(DialogStatus.NotApplicable, DialogStatusInput.NotApplicable)]
    [InlineData(DialogStatus.Awaiting, DialogStatusInput.Awaiting)]
    public void ToDialogStatusInput_MapsByName(DialogStatus source, DialogStatusInput expected)
    {
        Assert.Equal(expected, source.ToDialogStatusInput());
    }

    [Theory]
    [InlineData(DialogStatusInput.InProgress, DialogStatus.InProgress)]
    [InlineData(DialogStatusInput.Draft, DialogStatus.Draft)]
    [InlineData(DialogStatusInput.RequiresAttention, DialogStatus.RequiresAttention)]
    [InlineData(DialogStatusInput.Completed, DialogStatus.Completed)]
    [InlineData(DialogStatusInput.NotApplicable, DialogStatus.NotApplicable)]
    [InlineData(DialogStatusInput.Awaiting, DialogStatus.Awaiting)]
    public void ToDialogStatus_MapsSharedValuesByName(DialogStatusInput source, DialogStatus expected)
    {
        Assert.Equal(expected, source.ToDialogStatus());
    }

    [Theory]
    [InlineData(DialogStatusInput.New, DialogStatus.NotApplicable)]
    [InlineData(DialogStatusInput.Sent, DialogStatus.Awaiting)]
    public void ToDialogStatus_MapsInputOnlyValuesToNearestEquivalent(DialogStatusInput source, DialogStatus expected)
    {
        Assert.Equal(expected, source.ToDialogStatus());
    }
}
