using Digdir.Domain.Dialogporten.Application.Features.V1.Common.SystemLabels;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSystemLabels.Commands.BulkSet;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.ServiceOwner.DialogSystemLabels.Commands;

public class BulkSetSystemLabelCommandValidatorTests
{
    [Fact]
    public void Rejects_Multiple_System_Labels()
    {
        var command = new BulkSetSystemLabelCommand
        {
            EnduserId = "urn:altinn:person:identifier-no:01020312345",
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs = new[] { new DialogRevisionDto { DialogId = Guid.NewGuid() } },
                SystemLabels = new[] { SystemLabel.Values.Bin, SystemLabel.Values.Archive }
            }
        };

        var validator = new BulkSetSystemLabelCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Only one system label"));
    }

    [Fact]
    public void Accepts_Single_System_Label()
    {
        var command = new BulkSetSystemLabelCommand
        {
            EnduserId = "urn:altinn:person:identifier-no:01020312345",
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs = new[] { new DialogRevisionDto { DialogId = Guid.NewGuid() } },
                SystemLabels = new[] { SystemLabel.Values.Bin }
            }
        };

        var validator = new BulkSetSystemLabelCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
