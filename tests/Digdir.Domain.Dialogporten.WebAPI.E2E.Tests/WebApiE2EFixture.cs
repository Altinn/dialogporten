using Digdir.Library.Dialogporten.E2E.Common;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests;

public sealed class WebApiE2EFixture : E2EFixtureBase
{
    protected override bool IncludeGraphQlPreflight => false;
}
