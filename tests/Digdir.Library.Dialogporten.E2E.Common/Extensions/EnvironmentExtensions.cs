using System.Globalization;
using Microsoft.Extensions.Hosting;

namespace Digdir.Library.Dialogporten.E2E.Common.Extensions;

public static class EnvironmentExtensions
{
    extension(Environment)
    {
        public static string GetDotnetEnvironment() =>
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environments.Development;

        public static TokenGeneratorEnvironment GetTokenGeneratorEnvironment() =>
            Environment.GetDotnetEnvironment() switch
            {
                "Development" or "test" => TokenGeneratorEnvironment.At23,
                "staging" => TokenGeneratorEnvironment.Tt02,
                "yt01" => TokenGeneratorEnvironment.Yt01,
                _ => throw new InvalidOperationException($"Unknown environment: {Environment.GetDotnetEnvironment()}")
            };
    }
}
