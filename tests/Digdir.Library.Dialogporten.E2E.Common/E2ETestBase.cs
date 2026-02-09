namespace Digdir.Library.Dialogporten.E2E.Common;

public abstract class E2ETestBase<TFixture>(TFixture fixture) : IE2ETestHooks
    where TFixture : E2EFixtureBase
{
    protected TFixture Fixture { get; } = fixture;

    void IE2ETestHooks.BeforeTest() => Fixture.PreflightCheck();

    void IE2ETestHooks.AfterTest() => Fixture.Cleanup();
}
