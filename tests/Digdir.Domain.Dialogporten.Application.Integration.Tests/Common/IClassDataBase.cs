namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

/// <summary>
/// Base interface for integration-test scenarios.
/// Provides a display name used by xUnit for theory case names, which is shown as test text
/// in IDE test runners.
/// </summary>
public interface IClassDataBase
{
    /// <summary>
    /// Human-readable scenario name.
    /// </summary>
    string DisplayName { get; }
}
