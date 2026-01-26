namespace Digdir.Library.Dialogporten.E2E.Common;

public abstract class E2ETestBase<TFixture> : IE2ETestHooks
    where TFixture : E2EFixtureBase
{
    protected TFixture Fixture { get; }

    protected E2ETestBase(TFixture fixture) => Fixture = fixture;

    void IE2ETestHooks.BeforeTest() => Fixture.PreflightCheck();

    void IE2ETestHooks.AfterTest() => Fixture.CleanupAfterTest();
}
