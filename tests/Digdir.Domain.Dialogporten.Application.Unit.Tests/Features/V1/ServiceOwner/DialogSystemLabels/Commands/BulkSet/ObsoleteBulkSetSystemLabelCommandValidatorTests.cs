using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentAssertions;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.ServiceOwner.DialogSystemLabels.Commands.BulkSet;

public class ObsoleteBulkSetSystemLabelCommandValidatorTests
{
    private readonly BulkSetSystemLabelCommandValidator _validator = new();

    [Fact]
    public void Unique_DialogIds_Should_Be_Valid()
    {
        var command = new BulkSetSystemLabelCommand
        {
            EndUserId = "01017512345",
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
            EndUserId = "01017512345",
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
    public void Accepts_Multiple_System_Labels()
    {
        var command = new BulkSetSystemLabelCommand
        {
            EndUserId = "urn:altinn:person:identifier-no:01020312345",
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs = [new DialogRevisionDto { DialogId = Guid.NewGuid() }],
                SystemLabels = [SystemLabel.Values.Bin, SystemLabel.Values.Archive]
            }
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Accepts_Single_System_Label()
    {
        var command = new BulkSetSystemLabelCommand
        {
            EndUserId = "urn:altinn:person:identifier-no:01020312345",
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs = [new DialogRevisionDto { DialogId = Guid.NewGuid() }],
                SystemLabels = [SystemLabel.Values.Bin]
            }
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
