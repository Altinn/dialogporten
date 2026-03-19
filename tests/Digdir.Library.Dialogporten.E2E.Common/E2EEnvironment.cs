using Microsoft.Extensions.Hosting;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class E2EEnvironment
{
    public static string GetDotnetEnvironment() =>
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        ?? Environments.Development;

    public static string GetTokenGeneratorEnvironment() =>
        GetDotnetEnvironment() switch
        {
            "Development" or "test" => "at23",
            "staging" => "tt02",
            "yt01" => "yt01",
            _ => throw new InvalidOperationException($"Unknown environment: {GetDotnetEnvironment()}")
        };
}
