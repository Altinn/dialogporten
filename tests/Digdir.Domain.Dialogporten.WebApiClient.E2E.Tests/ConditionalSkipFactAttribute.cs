using Xunit;

namespace Digdir.Domain.Dialogporten.WebApiClient.E2E.Tests;

internal sealed class ConditionalSkipFactAttribute : FactAttribute
{
    private const string RunE2E = "RunE2E";

    public ConditionalSkipFactAttribute()
    {
        // var disableSkippingE2E = Environment.GetEnvironmentVariable(RunE2E);
        // if (disableSkippingE2E != "true")
        // {
        //     Skip = $"To run E2E tests, set the environment variable {RunE2E} to 'true'";
        // }
    }
}
