using AwesomeAssertions;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public sealed class WarmupSettingsTests
{
    // Deliberately pinned literals rather than the WarmupPhases constants: these tests guard the
    // registered phase budgets (db-pool 20 + ef-model 20 + service-resource-metadata 15, plus
    // end-user-search 15 when enabled) against accidental drift. If a budget changes on purpose,
    // update these numbers consciously.
    private const int MinimumWithEndUserSearch = 70;
    private const int MinimumWithoutEndUserSearch = 55;

    [Theory]
    [InlineData(MinimumWithEndUserSearch, true)]
    [InlineData(MinimumWithEndUserSearch - 1, true)]
    [InlineData(MinimumWithoutEndUserSearch, false)]
    [InlineData(MinimumWithoutEndUserSearch - 1, false)]
    public void WarmupSettingsValidator_Should_Reject_Run_Budget_At_Or_Below_Sum_Of_Phase_Budgets(
        int timeoutSeconds, bool runEndUserSearch)
    {
        var settings = new WarmupSettings
        {
            Enabled = true,
            TimeoutSeconds = timeoutSeconds,
            RunEndUserSearch = runEndUserSearch
        };

        var result = new WarmupSettingsValidator().Validate(settings);

        result.IsValid.Should().BeFalse();
        var expectedMinimum = runEndUserSearch ? MinimumWithEndUserSearch : MinimumWithoutEndUserSearch;
        result.Errors.Should().Contain(x =>
            x.PropertyName == nameof(WarmupSettings.TimeoutSeconds)
            && x.ErrorMessage.Contains($"({expectedMinimum}s"));
    }

    [Theory]
    [InlineData(MinimumWithEndUserSearch + 1, true)]
    [InlineData(80, true)] // The shipped appsettings value.
    [InlineData(MinimumWithoutEndUserSearch + 1, false)]
    [InlineData(80, false)]
    public void WarmupSettingsValidator_Should_Accept_Run_Budget_Above_Sum_Of_Phase_Budgets(
        int timeoutSeconds, bool runEndUserSearch)
    {
        var settings = new WarmupSettings
        {
            Enabled = true,
            TimeoutSeconds = timeoutSeconds,
            RunEndUserSearch = runEndUserSearch
        };

        var result = new WarmupSettingsValidator().Validate(settings);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void WarmupSettingsValidator_Should_Not_Apply_Phase_Budget_Rule_When_Warmup_Is_Disabled()
    {
        // With warmup disabled no phases run, so only the basic (0, 3600] bound applies. Hosts
        // that disable warmup (e.g. the Janitor) rely on this while keeping the 60s default.
        var settings = new WarmupSettings
        {
            Enabled = false,
            TimeoutSeconds = 10,
            RunEndUserSearch = true
        };

        var result = new WarmupSettingsValidator().Validate(settings);

        result.IsValid.Should().BeTrue();
    }
}
