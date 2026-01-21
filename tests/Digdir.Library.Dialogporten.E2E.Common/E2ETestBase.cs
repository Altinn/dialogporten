namespace Digdir.Library.Dialogporten.E2E.Common;

public abstract class E2ETestBase : IE2ETestHooks
{
    protected E2EFixture Fixture { get; }

    protected E2ETestBase(E2EFixture fixture)
    {
        Fixture = fixture;
    }

    void IE2ETestHooks.BeforeTest() => Fixture.PreflightCheck();

    void IE2ETestHooks.AfterTest() => Fixture.CleanupAfterTest();
}
