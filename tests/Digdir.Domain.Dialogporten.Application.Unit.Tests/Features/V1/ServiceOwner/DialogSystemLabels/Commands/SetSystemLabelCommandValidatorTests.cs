using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.DialogSystemLabels.Commands.Set;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.ServiceOwner.DialogSystemLabels.Commands;

public class SetSystemLabelCommandValidatorTests
{
    [Fact]
    public void Rejects_Multiple_System_Labels()
    {
        var command = new SetSystemLabelCommand
        {
            EndUserId = "urn:altinn:person:identifier-no:01020312345",
            SystemLabels = [SystemLabel.Values.Bin, SystemLabel.Values.Archive]
        };

        var validator = new SetSystemLabelCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Only one system label"));
    }

    [Fact]
    public void Accepts_Single_System_Label()
    {
        var command = new SetSystemLabelCommand
        {
            EndUserId = "urn:altinn:person:identifier-no:01020312345",
            SystemLabels = [SystemLabel.Values.Bin]
        };

        var validator = new SetSystemLabelCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
