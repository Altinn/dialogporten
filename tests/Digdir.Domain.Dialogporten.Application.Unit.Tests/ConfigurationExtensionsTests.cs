using System.Linq;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests;

public class ConfigurationExtensionsTests
{
    private const string RunningE2ETestsEnvironmentVariable = "RUNNING_E2E_TESTS";

    [Fact]
    public void AddLocalConfiguration_Should_Add_AppsettingsLocalJson_In_Development_When_Not_Running_E2E_Tests()
    {
        // Arrange
        using var _ = new EnvironmentVariableScope(RunningE2ETestsEnvironmentVariable, null);
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddLocalConfiguration(new TestHostEnvironment(Environments.Development));

        // Assert
        builder.Sources
            .OfType<JsonConfigurationSource>()
            .Select(x => x.Path)
            .Should()
            .ContainSingle(path => path == "appsettings.local.json");
    }

    [Fact]
    public void AddLocalConfiguration_Should_Not_Add_AppsettingsLocalJson_When_Running_E2E_Tests()
    {
        // Arrange
        using var _ = new EnvironmentVariableScope(RunningE2ETestsEnvironmentVariable, bool.TrueString);
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddLocalConfiguration(new TestHostEnvironment(Environments.Development));

        // Assert
        builder.Sources
            .OfType<JsonConfigurationSource>()
            .Select(x => x.Path)
            .Should()
            .NotContain(path => path == "appsettings.local.json");
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = nameof(ConfigurationExtensionsTests);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _variableName;
        private readonly string? _originalValue;

        public EnvironmentVariableScope(string variableName, string? value)
        {
            _variableName = variableName;
            _originalValue = Environment.GetEnvironmentVariable(variableName);
            Environment.SetEnvironmentVariable(variableName, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_variableName, _originalValue);
        }
    }
}
