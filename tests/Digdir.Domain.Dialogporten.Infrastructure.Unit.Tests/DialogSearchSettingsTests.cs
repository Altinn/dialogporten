using AwesomeAssertions;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public sealed class DialogSearchSettingsTests
{
    [Fact]
    public void DialogSearchSettings_Should_Default_Upsert_Command_Timeout_To_5()
    {
        var settings = new DialogSearchSettings();

        settings.UpsertCommandTimeoutSeconds.Should().Be(5);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(5)]
    public void DialogSearchSettingsValidator_Should_Accept_Non_Negative_Upsert_Command_Timeout(int? timeoutSeconds)
    {
        var settings = new DialogSearchSettings
        {
            UpsertCommandTimeoutSeconds = timeoutSeconds
        };

        var result = new DialogSearchSettingsValidator().Validate(settings);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DialogSearchSettingsValidator_Should_Reject_Negative_Upsert_Command_Timeout()
    {
        var settings = new DialogSearchSettings
        {
            UpsertCommandTimeoutSeconds = -1
        };

        var result = new DialogSearchSettingsValidator().Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.PropertyName == nameof(DialogSearchSettings.UpsertCommandTimeoutSeconds));
    }
}
