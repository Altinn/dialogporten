using Microsoft.Extensions.Hosting;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class Utils
{
    internal static string GetTokenGeneratorEnvironment()
    {
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environments.Development;

        return env switch
        {
            "Development" or "test" => "at23",
            "staging" => "tt02",
            "yt01" => "yt01",
            _ => throw new InvalidOperationException($"Unknown environment: {env}")
        };
    }
}
