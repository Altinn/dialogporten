namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

/// <summary>
/// Base record for integration-test scenarios.
/// Provides a display name used by xUnit for theory case names, which is shown as test text
/// in IDE test runners.
/// </summary>
/// <param name="DisplayName">Human-readable scenario name.</param>
public abstract record ClassDataBase(string DisplayName)
{
    /// <summary>
    /// Returns the scenario display name.
    /// </summary>
    public override string ToString() => DisplayName;
}
