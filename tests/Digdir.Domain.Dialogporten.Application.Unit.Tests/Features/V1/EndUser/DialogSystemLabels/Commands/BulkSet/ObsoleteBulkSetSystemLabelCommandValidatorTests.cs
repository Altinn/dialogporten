using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.EndUser.DialogSystemLabels.Commands.BulkSet;

public class ObsoleteBulkSetSystemLabelCommandValidatorTests
{
    private readonly BulkSetSystemLabelCommandValidator _validator = new(new BulkSetSystemLabelDtoValidator());

    [Fact]
    public void Unique_DialogIds_Should_Be_Valid()
    {
        var command = new BulkSetSystemLabelCommand
        {
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs =
                [
                    new DialogRevisionDto { DialogId = Guid.NewGuid() },
                    new DialogRevisionDto { DialogId = Guid.NewGuid() }
                ],
                SystemLabels = [SystemLabel.Values.Archive]
            }
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Duplicate_DialogIds_Should_Return_Error()
    {
        var id = Guid.NewGuid();
        var command = new BulkSetSystemLabelCommand
        {
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs =
                [
                    new DialogRevisionDto { DialogId = id },
                    new DialogRevisionDto { DialogId = id }
                ],
                SystemLabels = [SystemLabel.Values.Archive]
            }
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains(id.ToString(), result.Errors[0].ErrorMessage);
    }

    [Fact]
    public void Multiple_System_Labels_Should_Return_No_Errors()
    {
        var command = new BulkSetSystemLabelCommand
        {
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs = [new DialogRevisionDto { DialogId = Guid.NewGuid() }],
                SystemLabels = [SystemLabel.Values.Bin, SystemLabel.Values.Archive]
            }
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
