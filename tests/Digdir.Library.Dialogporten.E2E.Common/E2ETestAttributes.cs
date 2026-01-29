using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;

namespace Digdir.Library.Dialogporten.E2E.Common;

public static class E2EExplicitOptions
{
    // Flip to false when you want to
    // run/debug E2E tests in your IDE
    public const bool ExplicitTests = true;
}

public sealed class E2EFactAttribute : FactAttribute, IBeforeAfterTestAttribute
{
    public E2EFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        Explicit = E2EExplicitOptions.ExplicitTests;
    }

    public void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IE2ETestHooks hooks)
        {
            hooks.BeforeTest();
        }
    }

    public void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IE2ETestHooks hooks)
        {
            hooks.AfterTest();
        }
    }
}

public sealed class E2ETheoryAttribute : TheoryAttribute, IBeforeAfterTestAttribute
{
    public E2ETheoryAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        Explicit = E2EExplicitOptions.ExplicitTests;
    }

    public void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IE2ETestHooks hooks)
        {
            hooks.BeforeTest();
        }
    }

    public void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current.TestClassInstance is IE2ETestHooks hooks)
        {
            hooks.AfterTest();
        }
    }
}
