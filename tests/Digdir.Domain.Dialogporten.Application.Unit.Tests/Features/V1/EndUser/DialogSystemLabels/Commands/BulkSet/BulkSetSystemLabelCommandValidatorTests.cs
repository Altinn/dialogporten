using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogSystemLabels.Commands.BulkSet;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.SystemLabels;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Xunit;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.EndUser.DialogSystemLabels.Commands.BulkSet;

public class BulkSetSystemLabelCommandValidatorTests
{
    private readonly BulkSetSystemLabelCommandValidator _validator = new();

    [Fact]
    public void Unique_DialogIds_Should_Be_Valid()
    {
        var command = new BulkSetSystemLabelCommand
        {
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs = new[]
                {
                    new DialogRevisionDto { DialogId = Guid.NewGuid() },
                    new DialogRevisionDto { DialogId = Guid.NewGuid() }
                },
                SystemLabels = new[] { SystemLabel.Values.Archive }
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
                Dialogs = new[]
                {
                    new DialogRevisionDto { DialogId = id },
                    new DialogRevisionDto { DialogId = id }
                },
                SystemLabels = new[] { SystemLabel.Values.Archive }
            }
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains(id.ToString(), result.Errors[0].ErrorMessage);
    }
}
