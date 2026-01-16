namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;

public abstract class GraphQlE2ETestBase : IGraphQlE2ETestHooks
{
    protected GraphQlE2EFixture Fixture { get; }

    protected GraphQlE2ETestBase(GraphQlE2EFixture fixture)
    {
        Fixture = fixture;
    }

    void IGraphQlE2ETestHooks.BeforeTest() => Fixture.PreflightCheck();

    void IGraphQlE2ETestHooks.AfterTest() => Fixture.CleanupAfterTest();
}
